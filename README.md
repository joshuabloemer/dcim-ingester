# DCIM Ingester
Automatically detect new volumes that contain a DCIM folder and show a notification to allow the transfer of files to a target destination, organised into folders by date taken (from EXIF data).

# Usage
- Open the project in Visual Studio, restore the NuGet packages, and run the project.
- Right click on the icon in the system tray and set the destination directory.
- Connect an SD card.

# Dependencies
- MetadataExtractor

# Version History
- 1.0 (Dec 21, 2019) :: First completed version
- 1.1 (Dec XX, 2019) :: Fixed a bug where transfer progress did not update after the final file was transferred. Added checks for existence of the destination directory. Added a selectable list of directory structure formats.