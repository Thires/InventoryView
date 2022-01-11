![inventoryview](https://user-images.githubusercontent.com/28072996/148909614-f7f8e0f9-cbb8-467f-8abd-2ee6c009ce34.PNG)


# InventoryView
Added family vault<br>
Made it so you don't need vault book, will use vault standard<br>
Can still use vault book<br>
Will return books/register back to containers they are taken from<br>
Made search not case sensitive<br>
Removed a/an/some/several<br>
Made a second window where all searches will show up<br>
Added search count<br>
Added right click options for second window<br>

This plugin will record the items that each of your characters has in their inventory, vault (if you have a vault book), and home (if you have one) and make them searchable in a form.

Installation instructions:

Download InventoryView.zip
Install InventoryView.dll in either the Genie Plugins folder (%appdata%\Genie Client 3\Plugins) or your Genie.exe directory, whichever you keep config files in.
Either restart any open instances of Genie, or type: #plugin load InventoryView.dll ..in each open instance of Genie. 3a. Until this plugin is approved, you may get a warning saying you are installing an unapproved plugin the first time you load it.
Go to the Plugins menu and Inventory View.
Click the Scan Inventory button to record the inventory of the currently logged in character.
Inventory View form: The Inventory View form contains a tree list of each character that you've done an inventory scan. Each character splits into the inventory, vault and home as applicable and shows your containers and items in a tree structure that can be expanded or collapsed.

The buttons at the top allow you to search your inventory across all characters, highlights any items that match, and navigate through the results. You can also select an item and click Wiki Lookup to open Elanthipedia to the entry for that item, if an exact match was found. If no match was found it will do a search for the item.

If you are running multiple instances of Genie you will need to click the Reload File button on each instance of Genie after you have done an inventory scan on all of them. This will ensure that the inventory of each character is up-to-date in each instance of Genie.

When you scan the items of a character it will first check the inventory, then the vaults (if you have vault book or can do vault standard), family vault (if you have one) read register (if you have one), the home (if you have one) and Trader storage if you have ledger. At the end it will send "Scan Complete" to the screen. If for some reason the process gets stuck you won't see that text.

/InventoryView command (/iv command for short) /InventoryView scan -- scan the items on the current character. /InventoryView open -- open the InventoryView Window to see items.

Along with sending "Scan Complete" to the screen, the phrase "InventoryView scan complete" is sent to the parser at the end allowing you to do an inventory scan from a login or other script if desired.

send /InventoryView scan waitforre ^InventoryView scan complete
