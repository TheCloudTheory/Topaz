package topaz;

import com.microsoft.azure.storage.CloudStorageAccount;
import com.microsoft.azure.storage.blob.CloudBlobClient;
import com.microsoft.azure.storage.blob.CloudBlobContainer;
import com.microsoft.azure.storage.blob.BlobContainerProperties;
import org.junit.Test;

import static org.junit.Assert.*;

/**
 * Exercises the legacy azure-storage 7.x SDK (used by the Hadoop WASB connector)
 * against a running Topaz instance.
 *
 * Validates that GetContainerProperties returns the headers that the legacy SDK
 * requires (x-ms-lease-status, x-ms-lease-state, x-ms-has-immutability-policy,
 * x-ms-has-legal-hold).  Without them the SDK throws a NullPointerException in
 * ExecutionEngine.executeWithRetry and reports a spurious 404.
 *
 * The Topaz host is reachable at https://topaz.local.dev:8891 (blob endpoint)
 * via the Docker network bridge created by JavaFixture.
 */
public class BlobStorageLegacySdkTest {

    private static final String ACCOUNT_NAME = "wasbtestaccount";
    private static final String ARM_BASE     = "https://topaz.local.dev:8899";
    private static final String SUBSCRIPTION = System.getenv().getOrDefault("TOPAZ_SUBSCRIPTION_ID", "c0000001-0000-0000-0000-000000000001");
    private static final String RG           = "wasb-rg";
    private static final String CONTAINER    = "wasb-test-container";

    @Test
    public void getContainerProperties_returnsRequiredLeaseAndPolicyHeaders() throws Exception {
        String accountKey = createStorageAccountAndContainer();

        String connectionString =
                "DefaultEndpointsProtocol=https;" +
                "AccountName=" + ACCOUNT_NAME + ";" +
                "AccountKey=" + accountKey + ";" +
                "BlobEndpoint=https://" + ACCOUNT_NAME + ".blob.storage.topaz.local.dev:8891;";

        CloudStorageAccount account = CloudStorageAccount.parse(connectionString);
        CloudBlobClient client = account.createCloudBlobClient();
        CloudBlobContainer container = client.getContainerReference(CONTAINER);

        // downloadAttributes() issues GET /{container}?restype=container — the call
        // that previously caused a NullPointerException in the legacy SDK when the
        // emulator omitted required response headers.
        container.downloadAttributes();

        BlobContainerProperties props = container.getProperties();

        // The lease fields must be non-null; before the fix they were null because
        // the response headers were absent and the legacy SDK did not default them.
        assertNotNull("leaseStatus must not be null", props.getLeaseStatus());
        assertNotNull("leaseState must not be null", props.getLeaseState());
    }

    /**
     * Creates a subscription, resource group, storage account, and blob container
     * via the Topaz ARM REST API, then retrieves and returns the primary storage key.
     */
    private static String createStorageAccountAndContainer() throws Exception {
        put(ARM_BASE + "/subscriptions/" + SUBSCRIPTION + "/resourceGroups/" + RG +
                "?api-version=2021-04-01",
            "{\"location\":\"eastus\"}");

        put(ARM_BASE + "/subscriptions/" + SUBSCRIPTION + "/resourceGroups/" + RG +
                "/providers/Microsoft.Storage/storageAccounts/" + ACCOUNT_NAME +
                "?api-version=2023-01-01",
            "{\"location\":\"eastus\",\"kind\":\"StorageV2\"," +
            "\"sku\":{\"name\":\"Standard_LRS\"},\"properties\":{}}");

        put(ARM_BASE + "/subscriptions/" + SUBSCRIPTION + "/resourceGroups/" + RG +
                "/providers/Microsoft.Storage/storageAccounts/" + ACCOUNT_NAME +
                "/blobServices/default/containers/" + CONTAINER +
                "?api-version=2023-01-01",
            "{\"properties\":{}}");

        // Retrieve the actual key Topaz generated for the account.
        String keysUrl = ARM_BASE + "/subscriptions/" + SUBSCRIPTION + "/resourceGroups/" + RG +
                "/providers/Microsoft.Storage/storageAccounts/" + ACCOUNT_NAME +
                "/listKeys?api-version=2023-01-01";
        java.net.URL url = new java.net.URL(keysUrl);
        javax.net.ssl.HttpsURLConnection conn = openConnection(url, "POST", "{}");
        String body = readBody(conn);
        // parse first "value" from {"keys":[{"value":"<key>",...},...]}
        int start = body.indexOf("\"value\":\"") + "\"value\":\"".length();
        int end = body.indexOf("\"", start);
        return body.substring(start, end);
    }

    private static void put(String urlStr, String json) throws Exception {
        java.net.URL url = new java.net.URL(urlStr);
        openConnection(url, "PUT", json);
    }

    private static javax.net.ssl.HttpsURLConnection openConnection(
            java.net.URL url, String method, String json) throws Exception {

        javax.net.ssl.HttpsURLConnection conn =
                (javax.net.ssl.HttpsURLConnection) url.openConnection();
        conn.setRequestMethod(method);
        conn.setRequestProperty("Content-Type", "application/json");
        conn.setRequestProperty("Authorization", "Bearer " + getToken());
        conn.setConnectTimeout(10_000);
        conn.setReadTimeout(15_000);

        if (json != null) {
            conn.setDoOutput(true);
            try (java.io.OutputStream os = conn.getOutputStream()) {
                os.write(json.getBytes(java.nio.charset.StandardCharsets.UTF_8));
            }
        }

        int code = conn.getResponseCode();
        if (code >= 400) {
            throw new RuntimeException("ARM call failed: " + code + " " + url);
        }
        return conn;
    }

    /**
     * Fetches a real token from the Topaz Entra ID token endpoint using the
     * built-in topazadmin account (grant_type=password).
     */
    private static String getToken() throws Exception {
        String body = "grant_type=password" +
                "&username=topazadmin%40topaz.local.dev" +
                "&password=admin" +
                "&client_id=00000000-0000-0000-0000-000000000001" +
                "&scope=https%3A%2F%2Fmanagement.azure.com%2F.default";

        java.net.URL url = new java.net.URL(ARM_BASE + "/organizations/oauth2/v2.0/token");
        javax.net.ssl.HttpsURLConnection conn = (javax.net.ssl.HttpsURLConnection) url.openConnection();
        conn.setRequestMethod("POST");
        conn.setRequestProperty("Content-Type", "application/x-www-form-urlencoded");
        conn.setConnectTimeout(10_000);
        conn.setReadTimeout(15_000);
        conn.setDoOutput(true);
        try (java.io.OutputStream os = conn.getOutputStream()) {
            os.write(body.getBytes(java.nio.charset.StandardCharsets.UTF_8));
        }

        if (conn.getResponseCode() >= 400) {
            throw new RuntimeException("Token endpoint failed: " + conn.getResponseCode());
        }

        String response = readBody(conn);
        // parse access_token from {"access_token":"<token>",...}
        int start = response.indexOf("\"access_token\":\"") + "\"access_token\":\"".length();
        int end = response.indexOf("\"", start);
        return response.substring(start, end);
    }

    private static String readBody(javax.net.ssl.HttpsURLConnection conn) throws Exception {
        try (java.io.InputStream is = conn.getInputStream();
             java.util.Scanner scanner = new java.util.Scanner(is, "UTF-8")) {
            scanner.useDelimiter("\\A");
            return scanner.hasNext() ? scanner.next() : "";
        }
    }
}
