using UnityEngine;

public sealed class PlayerPrefsMgr : MonoSingle<PlayerPrefsMgr>
{
    public void SetInt(string key, int value, bool saveImmediately = true)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        PlayerPrefs.SetInt(key, value);
        if (saveImmediately)
        {
            PlayerPrefs.Save();
        }
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return string.IsNullOrWhiteSpace(key) ? defaultValue : PlayerPrefs.GetInt(key, defaultValue);
    }

    public void SetFloat(string key, float value, bool saveImmediately = true)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        PlayerPrefs.SetFloat(key, value);
        if (saveImmediately)
        {
            PlayerPrefs.Save();
        }
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        return string.IsNullOrWhiteSpace(key) ? defaultValue : PlayerPrefs.GetFloat(key, defaultValue);
    }

    public void SetString(string key, string value, bool saveImmediately = true)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        PlayerPrefs.SetString(key, value ?? string.Empty);
        if (saveImmediately)
        {
            PlayerPrefs.Save();
        }
    }

    public string GetString(string key, string defaultValue = "")
    {
        return string.IsNullOrWhiteSpace(key) ? defaultValue : PlayerPrefs.GetString(key, defaultValue);
    }

    public void SetBool(string key, bool value, bool saveImmediately = true)
    {
        SetInt(key, value ? 1 : 0, saveImmediately);
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        return GetInt(key, defaultValue ? 1 : 0) == 1;
    }

    public bool HasKey(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && PlayerPrefs.HasKey(key);
    }

    public void DeleteKey(string key, bool saveImmediately = true)
    {
        if (string.IsNullOrWhiteSpace(key) || !PlayerPrefs.HasKey(key))
        {
            return;
        }

        PlayerPrefs.DeleteKey(key);
        if (saveImmediately)
        {
            PlayerPrefs.Save();
        }
    }

    public void DeleteAll(bool saveImmediately = true)
    {
        PlayerPrefs.DeleteAll();
        if (saveImmediately)
        {
            PlayerPrefs.Save();
        }
    }

    public void Save()
    {
        PlayerPrefs.Save();
    }
}
