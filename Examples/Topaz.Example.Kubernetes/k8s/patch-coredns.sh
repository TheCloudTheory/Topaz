#!/bin/sh
# Inserts a CoreDNS rewrite rule so that any *.topaz.local.dev hostname
# resolves to the Topaz ClusterIP service.  The rule is idempotent — running
# the script twice leaves the ConfigMap unchanged.

set -e

MARKER="topaz.local.dev"
# Two rules: exact match for the ARM endpoint host, regex for all *.topaz.local.dev SDK hostnames.
REWRITE_RULE="    rewrite name exact topaz.local.dev topaz.topaz-system.svc.cluster.local\n    rewrite name regex (.+)\\\\.topaz\\\\.local\\\\.dev topaz.topaz-system.svc.cluster.local answer auto"

CURRENT=$(kubectl get configmap coredns -n kube-system -o jsonpath='{.data.Corefile}')

if echo "$CURRENT" | grep -q "$MARKER"; then
  echo "[coredns] Rewrite rule already present, skipping."
  exit 0
fi

# Insert the rewrite rule immediately before the first "forward ." line so it
# is evaluated inside the .:53 server block before queries leave the cluster.
PATCHED=$(echo "$CURRENT" | awk "/forward \\./{print \"$REWRITE_RULE\"}1")

kubectl patch configmap coredns -n kube-system \
  --type=merge \
  --patch "{\"data\":{\"Corefile\":$(printf '%s' "$PATCHED" | python3 -c 'import json,sys; print(json.dumps(sys.stdin.read()))')}}"

echo "[coredns] Rewrite rule added."
