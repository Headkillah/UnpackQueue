UnpackQueue 2.2
Written by Stefan Matsson (https://plus.google.com/111634125071893193016/about)
See License.txt for licensing information.
See Changelog.txt for the change log.

------
What is UnpackQueue?
------
UnpackQueue is an application for queueing unpacking of RAR archives in an easy way. 
It supports unpacking of individual files or all compressed files in a folder structure.
Unpacking is performed using UnRAR.exe from RarLabs which is a well known unpacking tool.

Features:
	* Builds a queue of compressed archives and unpacks them one by one.
	* Support for single archives or complete folder trees.
	* Integrates with the Windows context menu. Enables you to right click on a file/folder and select "Unpack queue".
	* Always unpacks files/folders to one specific folder (if specified).
	* Fully open source. Licenses under the MIT (Expat) license.

------
Configuration
------
In the root there is a configuration file called "UnpackQueue.exe.config".
This file contains all configuration available for UnpackQueue. 
Open the file in any text editor and edit the values. 
Comments on each option is found inside the config file.

------
Usage
------
If you installed the application you only need to right click the file/folder (or multiple files/folders) and click on "Unpack queue"
to add the file/folder to the queue. The unpacking will start automatically.

If you did NOT install the application you have two options:
1. Use the command line or Run and write:
   [path to UnpackQueue.exe] [path to file or folder to extract in quotation marks].
   Example: C:\Program Files\UnpackQueue\UnpackQueue.exe "C:\MyFile.rar"
   Note that several files/folders can be specified:
   C:\Program Files\UnpackQueue\UnpackQueue.exe "C:\MyFile.rar" "C:\MyFile2.rar" "C:\MyFolder" "C:\MyFile3.rar"

2. Mark the file or folder (multiple files/folders can be selected)
   and drag it on to the UnpackQueue.exe file (or a shortcut to the file).

------
Installation
------
First of all note that there is no actual need to install this application. 
See the "Usage" section for more information on how to use it without installing it.

To install the application just double click the exe and
press the two "Install" buttons (or just one of them depending on your needs). 
This will install UnpackQueue to the Windows context menu (the right click menu).

To uninstall double click the exe and click the "Uninstall" buttons.

------
Updating from a previous verion
------
If you are updating from a previous version just overwrite the "UnpackQueue.exe" file with
the one from the new version. You might also want to check the new config in case any options
has been added. All new options added are fully backwards compatible meaning that if you do not
add them to your config, UnpackQueue will behave the same way as in the previous version.
There is no need to re-install the Windows context menu feature.

------
Known issues
------

Issue: 
	The number of selected files/folders when right clicking on files/folders and selecting "Unpack queue" is restricted to a
	maximum of 9 items. The rest of the items are omitted. This is an issue with how the Windows registry handles 
	input parameters to applications.
Workaround: 
	1. Select less than than 9 items, click on "Unpack queue", select the rest of the items 
	(still needs to be less than 9) and select "Unpack queue" again.
	2. Use the command line approach (see the "Usage" section).

Issue:
	The installer adds the context menu to items not supported by UnRAR.exe (e.g. "tar.gz"). 
	This happens because the WinRAR registry entry does not distinguish different file types.

Workaround:
	Don't use the installer and chose "Open with" on all file types that you would like to open
	with UnpackQueue.