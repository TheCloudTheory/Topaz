Output verify-running.gif

Set Shell "bash"
Set FontSize 14
Set Width 900
Set Height 400
Set Theme "Dracula"
Set Padding 20

Hide
Type "topaz-host --log-level Warning &>/dev/null &"
Enter
Sleep 4s

Type "clear"
Enter
Show

Type "topaz health"
Enter
Sleep 3s

Hide
Type "kill %1 2>/dev/null"
Enter

Sleep 2s
