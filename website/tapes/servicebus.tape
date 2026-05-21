Output ../static/img/demos/servicebus.gif

Set Shell "bash"
Set FontSize 14
Set Width 1100
Set Height 400
Set Theme "Dracula"
Set Padding 24
Set TypingSpeed 80ms
Env PS1 "$ "

Hide
Type "set +m"
Enter
Type "mkdir -p /tmp/topaz-sb-demo && cd /tmp/topaz-sb-demo"
Enter
Type "topaz-host --log-level Error > /dev/null 2>&1 &"
Enter
Sleep 3s
Type "topaz subscription create --id 00000000-0000-0000-0000-000000000001 --name dev-local > /dev/null"
Enter
Type "topaz group create --name rg-my-app --location westeurope --subscription-id 00000000-0000-0000-0000-000000000001 > /dev/null"
Enter
Sleep 1s
Show

Ctrl+L
Sleep 200ms

Type "topaz servicebus namespace create --name sb-local --resource-group rg-my-app --subscription-id 00000000-0000-0000-0000-000000000001 --location westeurope"
Enter
Sleep 1s

Type "topaz servicebus queue create --queue-name my-queue --namespace-name sb-local --resource-group rg-my-app --subscription-id 00000000-0000-0000-0000-000000000001"
Enter

Sleep 3s

Hide
Type "kill %1 2>/dev/null; cd -; rm -rf /tmp/topaz-sb-demo"
Enter
Sleep 1s
Show
