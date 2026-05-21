Output ../static/img/demos/cli-help.gif

Set Shell "bash"
Set FontSize 14
Set Width 900
Set Height 700
Set Theme "Dracula"
Set Padding 24
Set TypingSpeed 80ms
Env PS1 "$ "

Hide
Type "set +m"
Enter
Type "mkdir -p /tmp/topaz-help-demo && cd /tmp/topaz-help-demo"
Enter
Type "topaz-host --log-level Error > /dev/null 2>&1 &"
Enter
Sleep 3s
Show

Ctrl+L
Sleep 200ms

Type "topaz -h"
Enter

Sleep 3s

Hide
Type "kill %1 2>/dev/null; cd -; rm -rf /tmp/topaz-help-demo"
Enter
Sleep 1s
Show
