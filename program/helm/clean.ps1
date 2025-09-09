helm uninstall gateway
helm uninstall loyalty
helm uninstall payment
helm uninstall reservation
helm uninstall identity
helm uninstall statistics
helm uninstall frontend

helm uninstall redis
helm uninstall kafka

helm uninstall reservation-db                                                                                           
helm uninstall loyalty-db  
helm uninstall payment-db                                                                  
helm uninstall identity-db
helm uninstall statistics-db

kubectl delete pvc -l app.kubernetes.io/name=postgresql