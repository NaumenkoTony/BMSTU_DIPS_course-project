Write-Host ">>> Starting Minikube..."
minikube start --driver=docker

kubectl create secret tls app-tls --cert=certs/tls.crt --key=certs/tls.key

Write-Host ">>> Enabling ingress addon..."
minikube addons enable ingress

Write-Host ">>> Deploying dependencies..."
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update

helm upgrade --install loyalty-db bitnami/postgresql -f ./postgres/values/loyalty-db.yaml
helm upgrade --install payment-db bitnami/postgresql -f ./postgres/values/payment-db.yaml
helm upgrade --install reservation-db bitnami/postgresql -f ./postgres/values/reservation-db.yaml
helm upgrade --install identity-db bitnami/postgresql -f ./postgres/values/identity-db.yaml
helm upgrade --install statistics-db bitnami/postgresql -f ./postgres/values/statistics-db.yaml

helm upgrade --install redis bitnami/redis -f ./redis/values/redis.yaml
helm upgrade --install kafka bitnami/kafka -f ./kafka/values/kafka.yaml

Write-Host ">>> Deploying microservices..."
helm upgrade --install gateway ./services -f ./services/values/gateway.yaml
helm upgrade --install loyalty ./services -f ./services/values/loyalty.yaml
helm upgrade --install payment ./services -f ./services/values/payment.yaml
helm upgrade --install reservation ./services -f ./services/values/reservation.yaml
helm upgrade --install identity ./services -f ./services/values/identity.yaml
helm upgrade --install statistics ./services -f ./services/values/statistics.yaml
helm upgrade --install frontend ./services -f ./services/values/frontend.yaml

Write-Host ">>> Checking status..."
kubectl get pods
kubectl get svc
kubectl get ingress

Write-Host ">>> Done!"
Write-Host "minikube ip"
