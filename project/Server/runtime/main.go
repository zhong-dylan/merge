package main

import (
	"context"
	"database/sql"
	"encoding/json"
	"fmt"
	"regexp"
	"strings"

	"github.com/heroiclabs/nakama-common/api"
	"github.com/heroiclabs/nakama-common/runtime"
)

const (
	playerProfileCollection = "player_profile"
	playerProfileKey        = "profile"
	startCurrency           = int64(500)
	welcomeNotificationCode = 1001
)

var usernamePattern = regexp.MustCompile(`^[A-Za-z0-9_-]{3,24}$`)

type playerProfile struct {
	UserID   string `json:"userId"`
	PlayerID string `json:"playerId"`
	Username string `json:"username"`
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
	if err := ensureBootstrapSchema(db); err != nil {
		return err
	}

	if err := initializer.RegisterAfterAuthenticateCustom(afterAuthenticateCustom); err != nil {
		return err
	}

	if err := initializer.RegisterRpc("get_player_profile", getPlayerProfile); err != nil {
		return err
	}

	logger.Info("bootstrap runtime loaded")
	return nil
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

	if created {
		_, _, err = nk.WalletUpdate(ctx, userID, map[string]int64{
			"gold":    startCurrency,
			"energy":  startCurrency,
			"diamond": startCurrency,
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
		"gold":       startCurrency,
		"energy":     startCurrency,
		"diamond":    startCurrency,
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
		INSERT INTO player_profiles(user_id, player_id, username)
		VALUES ($1, $2, $3)
	`, userID, nextValue, username); err != nil {
		return nil, false, err
	}

	if err := tx.Commit(); err != nil {
		return nil, false, err
	}

	return &playerProfile{
		UserID:   userID,
		PlayerID: fmt.Sprintf("%07d", nextValue),
		Username: username,
	}, true, nil
}

func loadPlayerProfile(ctx context.Context, db *sql.DB, userID string) (*playerProfile, error) {
	var numericID int64
	var username string

	err := db.QueryRowContext(ctx, `
		SELECT player_id, username
		FROM player_profiles
		WHERE user_id = $1
	`, userID).Scan(&numericID, &username)
	if err == sql.ErrNoRows {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}

	return &playerProfile{
		UserID:   userID,
		PlayerID: fmt.Sprintf("%07d", numericID),
		Username: username,
	}, nil
}
