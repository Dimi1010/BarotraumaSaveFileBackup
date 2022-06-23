# Barotrauma Save File Backup
Simple console application that creates backup save files with timestamp for Barotrauma when the game saves.

The application monitors and creates a timestamped copy of a save every time one is overwritten.

The location of the Barotrauma save folder can be set in appsettings.json.
Backups of singleplayer saves and multiplayer saves can be individually toggled in appsettings.json.

Warning:
The application can create a lot of backup save files if ran for a long time, use with caution.
