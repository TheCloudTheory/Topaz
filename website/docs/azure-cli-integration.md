---
sidebar_position: 4
---

# Azure CLI integration

Topaz offers a seamless integration with Azure CLI by exposing a dedicated cloud environment which you can add locally with just a few steps. Follow this guide for more details.

## Trusting the certificate
As Topaz exposes HTTPS endpoints using a self-signed certificate, Azure CLI may fail when connecting to it because it's not part of the CA bundle certificate file which it uses. If you face an error such as:
```bash
Caused by SSLError(SSLCertVerificationError(1, '[SSL: CERTIFICATE_VERIFY_FAILED] certificate verify failed: unable to get local issuer certificate
```
Please follow the [instruction](https://learn.microsoft.com/en-gb/cli/azure/use-azure-cli-successfully-troubleshooting?view=azure-cli-latest#work-behind-a-proxy) which explains how to use a self-signed certificate locally.

## Starting the emulator
Azure CLI requires you to authenticate against a real tenant before you can use the selected cloud environment. It will also try to obtain the metadata endpoints from it. To ensure the process goes smoothly, you will need to run the emulator in the background and include `--tenant-id` option:
```bash
$ topaz start --tenant-id <tenant-id>
```
It's important to remember that `--tenant-id` value must be an identifier of a tenant, where you have an account. 

:::tip[Best practice]

It's a good idea to create a dummy Microsoft Entra ID tenant so no additional security patterns are applied when signing-in (for instance Conditional Access). On the other hand using a real tenant may be helpful if you do want to implement E2E tests. Either option will work.
:::

Keep the emulator running in the background and continue with setting up the environment using Azure CLI.

## Creating a subscription
Topaz doesn't create a subscription by default yet (check [this](https://github.com/TheCloudTheory/Topaz/issues/16) issue for more information) so you will need to create one before you start integrating it with Azure CLI. The simplest option is using `subscription create` command provided by Topaz CLI:
```bash
$ topaz subscription create --id 36a28ebb-9370-46d8-981c-84efe02048ae --name "sub-local"
```
The same can be achieved using ASP.NET Core extension or with a raw HTTP request:
```bash
curl --location 'https://localhost:8899/subscriptions/36a28ebb-9370-46d8-981c-84efe02048ae' \
    --header 'Content-Type: application/json' \
    --data '{"subscriptionId":"36a28ebb-9370-46d8-981c-84efe02048ae",\
    "subscriptionName":"DEV-Local-Topaz"}'
```
You can also simplify this step by supplying the `--default-subscription` option when starting the emulator. Providing a subscription GUID to `topaz start` will cause the emulator to create that subscription automatically at startup, so you don't need to run `topaz subscription create` separately. Example:

```bash
$ topaz start --tenant-id <tenant-id> --default-subscription 36a28ebb-9370-46d8-981c-84efe02048ae
```

The subscription will be provisioned as the emulator comes up and will be immediately available for Azure CLI to use.

With the subscription created you can proceed to the next step.

## Setting up new environment
Azure CLI can connect with different cloud environments if needed (like Azure Stack) by registering them explictly using a dedicated command. To keep things simple, Topaz follows the exact same pattern. All you need is to download [this](https://raw.githubusercontent.com/TheCloudTheory/Topaz/refs/heads/main/cloud.json) file and run the following command:
```bash
$ az cloud register -n Topaz --cloud-config @"cloud.json"

Switched active cloud to 'Topaz'.
Use 'az login' to log in to this cloud.
Use 'az account set' to set the active subscription.
```

Now you can sign in using `az login` and start using Azure CLI commands as usual.

:::warning

Authentication to a local Entra ID tenant requires you to allow Azure CLI to authenticate to a tenant, which is not whitelisted. This is done by setting `AZURE_CORE_INSTANCE_DISCOVERY` environment variable to `false`. To keep things secure, make sure you restore the value to `true` after emulator is no longer needed.

:::

```bash
$ export AZURE_CORE_INSTANCE_DISCOVERY=false
$ az login
A web browser has been opened at https://topaz.local.dev:8899/organizations/oauth2/v2.0/authorize. Please continue the login in the web browser. If no web browser is available or if the web browser fails to open, use device code flow with `az login --use-device-code`.

Retrieving tenants and subscriptions for the selection...

[Tenant and subscription selection]

No     Subscription name    Subscription ID                       Tenant
-----  -------------------  ------------------------------------  -----------------------
[1] *  DEV-Local-Topaz      36a28ebb-9370-46d8-981c-84efe02048ae  Topaz Cloud Environment
```

## Changing the active environment
If you want to change the environment in Azure CLI, you can do that with `az cloud set` command:
```bash
$ az cloud set -n AzureCloud
```
You can change the environment anytime if needed. It won't affect the resources you created using Topaz (unless you're running it as a container with no volume attached).
