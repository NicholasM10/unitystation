Notes on how the context menu is setup:

-Gets right clicks from InputController.cs and calls a method in the UIManager
-UIManager instantiates a new context menu and removes the old one if it is there
-The new contextmenu runs a script to populate itself with buttons for each object passed in
-Each button should have a script to show and tell a subpanel to populate itself with buttons for each action you can 
take on that object.
-The submenu populates itself by searching each component of the target object that was passed in and using reflection
to check for the ContextMethod attribute. It takes the data from the attribute and populates itself based on that.

See following gist for other code that I wrote but may not have implemented/refactored yet:
https://gist.github.com/chilisource/4eb6d755fef752d5d1d46a9e0bf49cc7