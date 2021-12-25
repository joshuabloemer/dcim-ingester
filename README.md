# DCIM Ingester
A simple system tray application that ingests images from SD cards and sorts them into folders by date taken. The user is automatically prompted to start an ingest whenever an applicable volume is mounted to the system.

- The user is only prompted if the volume contains a DCIM folder and contains at least one ingestable file.
- Within DCIM, only folders that conform to the DCF specification are ingested from, which means non-image directories that cameras often create are ignored.
- Only files with a date taken EXIF attribute are sorted into folders by date taken. All other files are ingested into an "unsorted" folder.

# Usage
- Open the project in Visual Studio, restore the NuGet packages and run the project.
- Right click on the SD card icon in the system tray and open the settings.
	- Select the base destination directory, where all ingested files will go. Subfolders will be created as needed.
	- Select the subfolder structure, which determines how ingested files are organised by their date taken. Note that extra text can be added to the end of the name of the final folder in the structure and that folder will still be used by any subsequent ingests if needed.
- Connect an SD card and respond to the prompt.

# Version History
- 1.0 (Dec 21, 2019) -- First version.
- 1.1 (Jan 16, 2020)
	- Fixed a bug where the ingest progress did not update after the final file was ingested.
	- Added checks for existence of the destination directory.
	- Added a selectable list of destination directory structure formats.
	- Can now ingest all file names instead of only those covered by the DCF specification.
- 2.0 (Feb 15, 2021)
	- Major rework of the code including an upgrade to .NET 5.
	- A few small UI changes.
- 2.1 (Apr 28, 2021)
	- Significant change to the way volumes are detected.
	- Settings now opens at startup if the ingest destination is not set.
	- Drive labels are now included next to drive letters.
	- Fixed a bug where the ingest percentage would not display until the first file had been ingested.
	- Updated the SD card directory name regex to better comply with the DCF specification.
	- Various other assorted improvements.
	- Added some more code documentation.
- 2.2 (Dec 25, 2021)
	- Fixed a bug where the "Open Folder" button opens the source folder instead of the destination folder.