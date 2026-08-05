#!/bin/sh
# Provisions a local k3d cluster with Topaz as a shared emulator and deploys
# two sample applications that each use their own isolated Azure resources.

set -e

CLUSTER_NAME="topaz-demo"
CERT_FILE="../../certificate/topaz.crt"
KEY_FILE="../../certificate/topaz.key"

# Copy certs into each app build context so Docker can COPY them
cp "$CERT_FILE" apps/order-service/topaz.crt
cp "$CERT_FILE" apps/inventory-service/topaz.crt

# ── 2. k3d cluster ────────────────────────────────────────────────────────────
if k3d cluster list | grep -q "$CLUSTER_NAME"; then
  echo "[k3d] Cluster '$CLUSTER_NAME' already exists, skipping creation."
else
  echo "[k3d] Creating cluster '$CLUSTER_NAME'..."
  k3d cluster create "$CLUSTER_NAME" \
    --port "8899:30899@loadbalancer" \
    --wait
fi

kubectl config use-context "k3d-$CLUSTER_NAME"

# ── 3. Namespaces + Topaz TLS secret ─────────────────────────────────────────
echo "[k8s] Applying namespaces..."
kubectl apply -f k8s/namespace.yaml

echo "[k8s] Creating Topaz TLS secret..."
kubectl create secret generic topaz-tls \
  --from-file=topaz.crt="$CERT_FILE" \
  --from-file=topaz.key="$KEY_FILE" \
  -n topaz-system \
  --dry-run=client -o yaml | kubectl apply -f -

# ── 4. Deploy Topaz ───────────────────────────────────────────────────────────
echo "[k8s] Deploying Topaz..."
kubectl apply -f k8s/topaz.yaml
kubectl rollout status deployment/topaz -n topaz-system --timeout=120s

# ── 5. Patch CoreDNS ─────────────────────────────────────────────────────────
echo "[coredns] Patching CoreDNS to resolve *.topaz.local.dev..."
sh k8s/patch-coredns.sh
kubectl rollout restart deployment/coredns -n kube-system
kubectl rollout status deployment/coredns -n kube-system --timeout=60s

# ── 6. Build and import application images ───────────────────────────────────
echo "[docker] Building order-service..."
docker build -t order-service:latest apps/order-service/

echo "[docker] Building inventory-service..."
docker build -t inventory-service:latest apps/inventory-service/

echo "[k3d] Importing images into cluster..."
k3d image import order-service:latest inventory-service:latest -c "$CLUSTER_NAME"

# ── 7. Deploy applications ────────────────────────────────────────────────────
echo "[k8s] Deploying applications..."
kubectl apply -f k8s/apps.yaml

echo ""
echo "Done! Waiting for pods..."
kubectl get pods -A

echo ""
echo "Stream logs with:"
echo "  kubectl logs -n apps -l app=order-service -f"
echo "  kubectl logs -n apps -l app=inventory-service -f"
echo ""
echo "Tear down with: k3d cluster delete $CLUSTER_NAME"
