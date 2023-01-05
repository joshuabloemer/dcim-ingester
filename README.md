# DCIM Ingester
A system tray application that ingests images from SD cards and other storage media and sorts them into folders based on customizable rules with support for exif metadata. The user is automatically prompted to start an ingest whenever an applicable volume is mounted to the system.


# Usage
- Install the program with the provided MSI installer
- The Settings window will automatically open if the destination directory or the config file is not set. Otherwise, Settings can be accessed via the system tray icon's right-click menu (the icon is an SD card).
	- Set the destination directory to save all ingested files to. Subfolders will be created as necessary.
	- Select your config file containing the rules. (For a detailed explanation see [Rules](#rules))
	- Check the folder tree preview
- Connect an SD card and respond to the prompt.


# Rules

For every file that needs to be ingested the destination location must first be dertermined. This is where rules come in. Before the first rule is executed the destination path will simply be the destination directory set in the settings. Every rule that gets executed can then add something to the path until the last rules is executed and the file will be copied to the destination.

## What is a rule?
Every rules is made out of two parts: a condition and a path, separated by a single space.
```apache
condition path
```
If the condition is met then then the path is appended to the already existing destination path.

## Conditions
Conditions are used to determine if the path of a rule should be appended to the destination path of the file. If the condition evaluates to true then the path of the rule will get appended to the destination path.

### `any`
The any condition will always be true meaning the path will always be appended to the destination path.
### `arg1 = arg2`
True if the first argument is equal to the second argument
### `arg1 != arg2`
The opposite of `arg1 = arg2`
### `arg1 contains arg2`
True if argument 2 is in argument 1 

Example: `"Dcim Ingester" contains "m Ing"` --> true
\
\
\
\
The following conditions try to convert both arguments to floating point numbers. Use with caution. If one or both arguments can't be converted the ingest will fail.
### `arg1 < arg2`
### `arg1 <= arg2`
### `arg1 > arg2`
### `arg1 >= arg2`


## Condition arguments
You might have noticed that every condition except `any` has two arguments. These arguments are always strings.

### `"string"`
Will evaluate to string.

### `year`/`month`/`day`/`hour`/`minute`/`second`

These arguments will evaluate to the time the file was created. If the file has exif metadata tags that contain the date the image was taken (more specifically the Date Time Original tag). If not it will default to either the "Created" or the "Modified" property of the file (whichever is older).

### `file name`
Will evaluate to the name of the file that is currently being ingested (without the file extension).

### `extension`
Will evaluate to the extension of the file that is currently being ingested (without the period).

### `path`
Will evaluate to the original path of the file currently being ingested.

### `path[i]`/`path[-i]`
Will evaluate to the ith directory of the original path (zero indexed). -1 is the first directory from the end, -2 the second, etc.

### `metaDataDirectory;metaDataTag;`
This argument allows full access to a files (exif) metadata. `metaDataDirectory` is the directory the tag is in and `metaDataTag` is the tag to get. A tool to list metadata directories and tags of a file is available [here](https://github.com/joshuabloemer/metadata-viewer/releases). This argument will evaluate to `null` if the file has no metadata, the directory doesn't exist or the tag doesn't exist.

## Path
The path of a rule contains the directories to be appended to the destination path when the condition is true. The directories are separated by slashes (`/`) and the whole path is contained inside double quotes (`"`). The first slash is optional.
```python
"dir1/dir2"
"/dir1/dir2"
```
are both valid paths

All arguments can also be used inside paths by enclosing them in braces

For example: `any "{year}/{month}/{day}"`

will sort all files into folders by date taken.

The path can also be conpletely left out. If a rule without a path is matched it will advance to the next indentation level.

## Chaining rules together
Rules can be chained together to allow for more complex sorting.


### Sequences

If two or more rules are placed below each other with the same indentaion level the first one that matches will be executed. All other rules will be ignored
```python
extension = "DNG" "/raw"

extension = "jpg" "/jpeg"

...
```
Will place all files with .DNG extension in the folder raw and all files with the .jpg extension in the folder jpeg. If no top level rule matches the file will be placed into /Unsorted (having `any "/custom unsorted directory"` as the last rule effectively overwrites this with a custom directory)

### Indentation
If a rules matches all rules that are indented by one level more (4 spaces) than the matching rule will also attempt to match as described in [Sequences](#sequences). There is no limit to the number of indents.

```python
extension = "jpg" "/jpg"
    path contains "Panorama" "/panorama"
		...

	year = "2022" "/last year"
		...
```

Will place all files with the .jpg extension in the folder `jpg/panorama` if the original file path contained the string Panorama, all files with the .jpg extension that were taken in 2022 in the folder `/jpeg/last year` folder if the original path didn't contain the string Panorama and all other files with the .jpg extension in the folder `/jpeg` .


### Connected sequences
If two or more rules are placed directly below each other any rule that matches will then try to match the next indented rule.

```python
extension = "DNG" "/raw"
extension = "jpg" "/jpeg"
	any "/{year}/{month}/{day}"
```
Will place all files with the .DNG extension in `/raw/year/month/day` and all files with the .jpg extension in `/jpeg/year/month/day` where year, month and day are the year, month and date the file was created. 

This is equivalent to:
```python
extension="DNG" "/raw/{year}/{month}/{day}"

extension="jpg" "/jpeg/{year}/{month}/{day}"
```

## Examples
```python
any "/{year}/{month}/{day}"
```
Will place all files into directories based on the file creation time
```python
extension = "MP4" "/videos"
extension = "DNG" "/photos/raw"
extension = "JPG" "/photos/jpeg"
	any "/{year}/{month}/{day}"
		Exif IFD0;Model; = "null" 
		any "/{Exif IFD0;Model;}"

any "/other files"
```
Will separate MP4, DNG and JPG files and then place them into sub folders based on date taken. If the camera model tag is present the file will be placed into a sub directory with the model name as it's name. All other files are placed in the "other files" directory.

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
- 3.0 (Jan 1, 2023)
    - Added a new System to sort files based on metadata
    - Added an installer
    - Added a folder tree preview window to the settings window
    - Made application single instance
    - Update Readme to include a detailed explanation of the rule system
