Output ../static/img/demos/subscription-and-group.gif

Set Shell "bash"
Set FontSize 14
Set Width 1100
Set Height 380
Set Theme "Dracula"
Set Padding 24
Set TypingSpeed 80ms
Env PS1 "$ "

Hide
Type "set +m"
Enter
Type "mkdir -p /tmp/topaz-sub-demo && cd /tmp/topaz-sub-demo"
Enter
Type "topaz-host --log-level Error > /dev/null 2>&1 &"
Enter
Sleep 3s
Show

Ctrl+L
Sleep 200ms

Type "topaz subscription create --id 00000000-0000-0000-0000-000000000001 --name dev-local"
Enter
Sleep 1s

Type "topaz group create --name rg-my-app --location westeurope --subscription-id 00000000-0000-0000-0000-000000000001"
Enter

Sleep 3s

Hide
Type "kill %1 2>/dev/null; cd -; rm -rf /tmp/topaz-sub-demo"
Enter
Sleep 1s
Show
