# DCIM Ingester
A simple system tray application that ingests images from SD cards and sorts them into folders by date taken. The user is automatically prompted to start an ingest whenever an applicable volume is mounted to the system.

- The user is only prompted if the volume contains a DCIM folder and contains at least one ingestable file.
- Within DCIM, only folders that conform to the DCF specification are ingested from, which means non-image directories that cameras often create are ignored.
- Only files with a date taken EXIF attribute are sorted into folders by date taken. All other files are ingested into an "Unsorted" folder.

# Usage
- Open the project in Visual Studio, restore the NuGet packages and run the project.
- The Settings window will automatically open if the destination directory is not set. Otherwise, Settings can be accessed via the system tray icon's right-click menu (the icon is an SD card).
	- Set the destination directory to save all ingested files to. Subfolders will be created as necessary.
	- Select how ingested files should be organised into folders by their date taken. Note that extra text can be added (following a space) to the end of the name of the final folder in the path and that folder will still be used by any subsequent ingests if it is needed.
- Connect an SD card and respond to the prompt.

# Version History
- 1.0 (Dec 21, 2019) -- First version
- 1.1 (Jan 16, 2020)
	- Fixed an issue where the ingest progress did not update after the final file was ingested
	- Added checks for existence of the destination directory
	- Added a selectable list of destination directory structure formats
	- Can now ingest all file names instead of only those covered by the DCF specification
- 2.0 (Feb 15, 2021)
	- Major rework of the code including an upgrade to .NET 5
	- A few small UI changes
- 2.1 (Apr 28, 2021)
	- Significant change to the way volumes are detected
	- Settings now opens at startup if the ingest destination is not set
	- Drive labels are now included next to drive letters
	- Fixed an issue where the ingest percentage would not display until the first file had been ingested
	- Updated the SD card directory name regex to better comply with the DCF specification
	- Various other assorted improvements
	- Added some more code documentation
- 2.2 (Dec 25, 2021)
	- Fixed a bug where the "Open Folder" button opens the source folder instead of the destination folder.
- 2.3 (May 23, 2022)
	- Fixed a Bug where injecting would hang if the timezone was set to anything other than UTC
	- Added support for files with multiple  EXIF SubIFD directories (DNG files created by DJI drones)
	- Changed the way duplicate files are detected. Files with different name but same metadata or files with same name and same metadata as already existing files will not be ingested   
- 2.4 (June 6, 2022)
	- Changed the Volume Watcher to respond to sd card inserts into already attached reader 
- 2.5 (Sep 11, 2022)
	- Reimplemented volume detection using SHChangeNotifyRegister.
	- Fixed an issue where cancelling the Settings window without having an ingest destination set (e.g. on first run) would throw an exception
	- Ingests can now happen if Settings is open, and Settings can now be opened if an ingest is in progress
	- The window now automatically repositions if display settings are changed (resolution, display removal, etc.)
	- Restricted Settings from being able to be opened multiple times simultaneously
	- Various other small improvements