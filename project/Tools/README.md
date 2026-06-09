# Luban Tools

- `export_code.bat` / `export_code.sh`
  Export client C# config code, server Go config code, and shared binary data.
  Client code output: `Assets/Gen/Luban/Client`
  Server code output: `Server/Gen/LubanGo`
  Data output: `Assets/Addressables_Remote/Config/Bytes`

- `export_json.bat` / `export_json.sh`
  Export client JSON C# config code, client JSON data, server Go JSON config code, and server JSON data.
  Client code output: `Assets/Gen/Luban/Json`
  Client data output: `Assets/Addressables_Remote/Config/Json`
  Server code output: `Server/Gen/LubanGoJson`
  Server data output: `Server/Config/Json`

- `restart_server.bat` / `restart_server.sh`
  Restart local Nakama server containers from project root.

- Config root:
  `Config/Luban`

- Luban tool binary:
  `Luban/Luban.dll`

- Runtime note:
  Current scripts enable `DOTNET_ROLL_FORWARD=Major`, so Luban built for .NET 8 can run on a machine with only .NET 9 installed.
