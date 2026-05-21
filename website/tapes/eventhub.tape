Output ../static/img/demos/eventhub.gif

Set Shell "bash"
Set FontSize 14
Set Width 1100
Set Height 400
Set Theme "Dracula"
Set Padding 24
Set TypingSpeed 80ms
Env PS1 "$ "

Hide
Set TypingSpeed 1ms
Type "set +m"
Enter
Type "mkdir -p /tmp/topaz-eh-demo && cd /tmp/topaz-eh-demo"
Enter
Type "topaz-host --log-level Error > /dev/null 2>&1 &"
Enter
Sleep 3s
Type "topaz subscription create --id 00000000-0000-0000-0000-000000000001 --name dev-local > /dev/null"
Enter
Type "topaz group create --name rg-my-app --location westeurope --subscription-id 00000000-0000-0000-0000-000000000001 > /dev/null"
Enter
Sleep 1s
Set TypingSpeed 80ms
Show

Ctrl+L
Sleep 200ms

Type "topaz eventhubs namespace create --name eh-local --resource-group rg-my-app --subscription-id 00000000-0000-0000-0000-000000000001 --location westeurope"
Enter
Sleep 1s

Type "topaz eventhubs eventhub create --name my-hub --namespace-name eh-local --resource-group rg-my-app --subscription-id 00000000-0000-0000-0000-000000000001"
Enter

Sleep 3s

Hide
Set TypingSpeed 1ms
Type "kill %1 2>/dev/null; cd -; rm -rf /tmp/topaz-eh-demo"
Enter
Sleep 1s
Show
