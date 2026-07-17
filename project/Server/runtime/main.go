package main

import (
	"context"
	"database/sql"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"strconv"
	"strings"

	"github.com/heroiclabs/nakama-common/api"
	"github.com/heroiclabs/nakama-common/runtime"
)

const (
	playerProfileCollection = "player_profile"
	playerProfileKey        = "profile"
	defaultInitGold         = int64(500)
	defaultInitDiamond      = int64(50)
	defaultInitEnergy       = int64(100)
	welcomeNotificationCode = 1001
	serverConfigDir         = "/nakama/data/modules/runtime/Config/Json"
)

var usernamePattern = regexp.MustCompile(`^[A-Za-z0-9_-]{3,24}$`)
var loadedConfigs map[string][]map[string]interface{}
var currentServerVersion int

type playerProfile struct {
	UserID           string `json:"userId"`
	PlayerID         string `json:"playerId"`
	Username         string `json:"username"`
	MaxServerVersion int    `json:"maxServerVersion"`
}

type playerBootstrap struct {
	UserID   string `json:"userId"`
	PlayerID string `json:"playerId"`
	Username string `json:"username"`
	Gold     int64  `json:"gold"`
	Energy   int64  `json:"energy"`
	Diamond  int64  `json:"diamond"`
}

func InitModule(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, initializer runtime.Initializer) error {
	version, err := readServerVersion()
	if err != nil {
		logger.Error("read server version failed: %v", err)
		return err
	}
	currentServerVersion = version

	tables, err := loadAllConfigs()
	if err != nil {
		logger.Error("load luban configs failed: %v", err)
		return err
	}
	loadedConfigs = tables

	if err := ensureBootstrapSchema(db); err != nil {
		return err
	}

	if err := initializer.RegisterAfterAuthenticateCustom(afterAuthenticateCustom); err != nil {
		return err
	}

	if err := initializer.RegisterRpc("get_player_profile", getPlayerProfile); err != nil {
		return err
	}

	logger.Info("bootstrap runtime loaded, configs ready, serverVersion=%d", currentServerVersion)
	return nil
}

func readServerVersion() (int, error) {
	rawVersion := strings.TrimSpace(os.Getenv("RELEASE_VERSION"))
	if rawVersion == "" {
		return 0, fmt.Errorf("RELEASE_VERSION is empty")
	}

	version, err := strconv.Atoi(rawVersion)
	if err != nil || version <= 0 {
		return 0, fmt.Errorf("RELEASE_VERSION must be a positive integer: %s", rawVersion)
	}

	return version, nil
}

func loadAllConfigs() (map[string][]map[string]interface{}, error) {
	configs := make(map[string][]map[string]interface{})

	entries, err := os.ReadDir(serverConfigDir)
	if err != nil {
		if os.IsNotExist(err) {
			return configs, nil
		}
		return nil, fmt.Errorf("read config dir %s failed: %w", serverConfigDir, err)
	}

	for _, entry := range entries {
		if entry.IsDir() || filepath.Ext(entry.Name()) != ".json" {
			continue
		}

		tableName := strings.TrimSuffix(entry.Name(), ".json")
		fullPath := filepath.Join(serverConfigDir, entry.Name())
		content, err := os.ReadFile(fullPath)
		if err != nil {
			return nil, fmt.Errorf("read config %s failed: %w", fullPath, err)
		}

		var rows []map[string]interface{}
		if err := json.Unmarshal(content, &rows); err != nil {
			return nil, fmt.Errorf("parse config %s failed: %w", fullPath, err)
		}

		configs[tableName] = rows
	}

	return configs, nil
}

func getGlobalConfigInt64(key string, defaultValue int64) int64 {
	rows, ok := loadedConfigs["global_tbconfig"]
	if !ok {
		return defaultValue
	}

	for _, row := range rows {
		rawKey, ok := row["key"].(string)
		if !ok || rawKey != key {
			continue
		}

		rawValue, ok := row["value"].(string)
		if !ok {
			return defaultValue
		}

		value, err := strconv.ParseInt(rawValue, 10, 64)
		if err != nil {
			return defaultValue
		}

		return value
	}

	return defaultValue
}

func afterAuthenticateCustom(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, out *api.Session, in *api.AuthenticateCustomRequest) error {
	userID, _ := ctx.Value(runtime.RUNTIME_CTX_USER_ID).(string)
	if userID == "" {
		return runtime.NewError("missing user id", 13)
	}

	username := strings.TrimSpace(in.GetUsername())
	if !usernamePattern.MatchString(username) {
		return runtime.NewError("username must be 3-24 chars using letters, numbers, underscore, or hyphen", 3)
	}

	profile, created, err := ensurePlayerProfile(ctx, db, userID, username)
	if err != nil {
		logger.Error("ensure player profile failed: %v", err)
		return runtime.NewError("failed to initialize player profile", 13)
	}

	if err := ensurePlayerServerVersion(ctx, logger, db, userID, profile); err != nil {
		return err
	}

	if err := nk.AccountUpdateId(ctx, userID, username, map[string]interface{}{}, "", "", "", "", ""); err != nil {
		logger.Error("account update failed for %s: %v", userID, err)
		return runtime.NewError("failed to update username", 13)
	}

	profileJSON, err := json.Marshal(profile)
	if err != nil {
		return runtime.NewError("failed to encode player profile", 13)
	}

	_, err = nk.StorageWrite(ctx, []*runtime.StorageWrite{
		{
			Collection:      playerProfileCollection,
			Key:             playerProfileKey,
			UserID:          userID,
			Value:           string(profileJSON),
			PermissionRead:  0,
			PermissionWrite: 0,
		},
	})
	if err != nil {
		logger.Error("storage write failed for %s: %v", userID, err)
		return runtime.NewError("failed to save player profile", 13)
	}

	initGold := getGlobalConfigInt64("init_gold", defaultInitGold)
	initDiamond := getGlobalConfigInt64("init_diamond", defaultInitDiamond)
	initEnergy := getGlobalConfigInt64("init_energy", defaultInitEnergy)

	if created {
		_, _, err = nk.WalletUpdate(ctx, userID, map[string]int64{
			"gold":    initGold,
			"energy":  initEnergy,
			"diamond": initDiamond,
		}, map[string]interface{}{
			"reason": "first_login_bootstrap",
		}, true)
		if err != nil {
			logger.Error("wallet bootstrap failed for %s: %v", userID, err)
			return runtime.NewError("failed to initialize player wallet", 13)
		}
	}

	subject := "登录成功"
	if created {
		subject = "欢迎来到游戏"
	}

	err = nk.NotificationSend(ctx, userID, subject, map[string]interface{}{
		"userId":     profile.UserID,
		"playerId":   profile.PlayerID,
		"username":   profile.Username,
		"firstLogin": created,
		"gold":       initGold,
		"energy":     initEnergy,
		"diamond":    initDiamond,
	}, welcomeNotificationCode, "", true)
	if err != nil {
		logger.Error("welcome notification failed for %s: %v", userID, err)
		return runtime.NewError("failed to send welcome notification", 13)
	}

	logger.Info("player bootstrap complete user=%s playerId=%s created=%v", userID, profile.PlayerID, created)
	return nil
}

func getPlayerProfile(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, payload string) (string, error) {
	userID, _ := ctx.Value(runtime.RUNTIME_CTX_USER_ID).(string)
	if userID == "" {
		return "", runtime.NewError("missing user id", 13)
	}

	profile, err := loadPlayerProfile(ctx, db, userID)
	if err != nil {
		logger.Error("load player profile failed: %v", err)
		return "", runtime.NewError("failed to load player profile", 13)
	}
	if profile == nil {
		return "", runtime.NewError("player profile not found", 5)
	}

	if err := ensurePlayerServerVersion(ctx, logger, db, userID, profile); err != nil {
		return "", err
	}

	account, err := nk.AccountGetId(ctx, userID)
	if err != nil {
		logger.Error("account get failed for %s: %v", userID, err)
		return "", runtime.NewError("failed to load account", 13)
	}

	wallet := map[string]int64{}
	if account.Wallet != "" {
		if err := json.Unmarshal([]byte(account.Wallet), &wallet); err != nil {
			logger.Error("wallet parse failed for %s: %v", userID, err)
			return "", runtime.NewError("failed to parse wallet", 13)
		}
	}

	responseJSON, err := json.Marshal(&playerBootstrap{
		UserID:   profile.UserID,
		PlayerID: profile.PlayerID,
		Username: profile.Username,
		Gold:     wallet["gold"],
		Energy:   wallet["energy"],
		Diamond:  wallet["diamond"],
	})
	if err != nil {
		return "", runtime.NewError("failed to encode bootstrap payload", 13)
	}

	return string(responseJSON), nil
}

func ensureBootstrapSchema(db *sql.DB) error {
	queries := []string{
		`CREATE TABLE IF NOT EXISTS player_id_sequence (
			name TEXT PRIMARY KEY,
			next_value BIGINT NOT NULL
		)`,
		`CREATE TABLE IF NOT EXISTS player_profiles (
			user_id UUID PRIMARY KEY,
			player_id BIGINT NOT NULL UNIQUE,
			username TEXT NOT NULL
		)`,
		`ALTER TABLE player_profiles
			ADD COLUMN IF NOT EXISTS max_server_version INTEGER NOT NULL DEFAULT 0`,
	}

	for _, query := range queries {
		if _, err := db.Exec(query); err != nil {
			return err
		}
	}

	return nil
}

func ensurePlayerProfile(ctx context.Context, db *sql.DB, userID string, username string) (*playerProfile, bool, error) {
	existing, err := loadPlayerProfile(ctx, db, userID)
	if err != nil {
		return nil, false, err
	}
	if existing != nil {
		if existing.Username != username {
			if _, err := db.ExecContext(ctx, `UPDATE player_profiles SET username = $2 WHERE user_id = $1`, userID, username); err != nil {
				return nil, false, err
			}
			existing.Username = username
		}
		return existing, false, nil
	}

	tx, err := db.BeginTx(ctx, nil)
	if err != nil {
		return nil, false, err
	}
	defer tx.Rollback()

	if _, err := tx.ExecContext(ctx, `
		INSERT INTO player_id_sequence(name, next_value)
		VALUES ('default', (SELECT COUNT(*) FROM users))
		ON CONFLICT (name) DO NOTHING
	`); err != nil {
		return nil, false, err
	}

	var nextValue int64
	if err := tx.QueryRowContext(ctx, `
		UPDATE player_id_sequence
		SET next_value = next_value + 1
		WHERE name = 'default'
		RETURNING next_value
	`).Scan(&nextValue); err != nil {
		return nil, false, err
	}

	if _, err := tx.ExecContext(ctx, `
		INSERT INTO player_profiles(user_id, player_id, username, max_server_version)
		VALUES ($1, $2, $3, $4)
	`, userID, nextValue, username, currentServerVersion); err != nil {
		return nil, false, err
	}

	if err := tx.Commit(); err != nil {
		return nil, false, err
	}

	return &playerProfile{
		UserID:           userID,
		PlayerID:         fmt.Sprintf("%07d", nextValue),
		Username:         username,
		MaxServerVersion: currentServerVersion,
	}, true, nil
}

func ensurePlayerServerVersion(ctx context.Context, logger runtime.Logger, db *sql.DB, userID string, profile *playerProfile) error {
	if profile.MaxServerVersion > currentServerVersion {
		logger.Warn(
			"reject lower server login user=%s playerId=%s maxServerVersion=%d currentServerVersion=%d",
			userID,
			profile.PlayerID,
			profile.MaxServerVersion,
			currentServerVersion)
		return runtime.NewError("player has already entered a higher version server", errorCodeServerVersionDowngradeForbidden)
	}

	if currentServerVersion <= profile.MaxServerVersion {
		return nil
	}

	if err := updatePlayerMaxServerVersion(ctx, db, userID, currentServerVersion); err != nil {
		logger.Error("update max server version failed for %s: %v", userID, err)
		return runtime.NewError("failed to update player server version", 13)
	}

	profile.MaxServerVersion = currentServerVersion
	return nil
}

func loadPlayerProfile(ctx context.Context, db *sql.DB, userID string) (*playerProfile, error) {
	var numericID int64
	var username string
	var maxServerVersion int

	err := db.QueryRowContext(ctx, `
		SELECT player_id, username, max_server_version
		FROM player_profiles
		WHERE user_id = $1
	`, userID).Scan(&numericID, &username, &maxServerVersion)
	if err == sql.ErrNoRows {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}

	return &playerProfile{
		UserID:           userID,
		PlayerID:         fmt.Sprintf("%07d", numericID),
		Username:         username,
		MaxServerVersion: maxServerVersion,
	}, nil
}

func updatePlayerMaxServerVersion(ctx context.Context, db *sql.DB, userID string, maxServerVersion int) error {
	_, err := db.ExecContext(ctx, `
		UPDATE player_profiles
		SET max_server_version = $2
		WHERE user_id = $1
	`, userID, maxServerVersion)
	return err
}
