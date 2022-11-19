# Barotrauma Save File Backup
Simple console application that creates timestamped backup saves for Barotrauma when the game saves.

The application monitors the barotrauma save folder. Every time the game writes or updates a save, the application creates a timestamped archive containing the updated save.

Application can be configured through appsettings.json:
- BarotraumaSaveFileFolder: Controls the location which the application monitors. Should point to the Barotrauma Save Folder.
- BarotraumaBackupFolder: Controls the location where the backups are produced. Uses the location of the save file that is being backed up when empty.
- BackupSingleplayerSaves: Controls the backup behaviour for singleplayer saves. If set to false the application ignores singleplayer saves.
- BackupMultiplayerSaves: Controls the backup behaviour for multiplayer saves. If set to false the application ignores multiplayer saves.

Portable version requires .NET 6 to be installed on the target system.

Warning:
As the game saves on every campaign map change, the application can create a lot of backups if ran for a long time. Use with caution.
