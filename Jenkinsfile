pipeline {
    agent any

    environment {
        IMAGE_NAME = "ecommerce-api"
        CONTAINER_NAME = "ecommerce-api"
    }

    stages {
        stage('Build') {
            steps {
                sh 'docker build -t ${IMAGE_NAME} -f ECommerceAPI/Dockerfile .'
            }
        }

        stage('Deploy') {
            steps {
                withCredentials([
                    string(credentialsId: 'STRIPE_SECRET_KEY',      variable: 'STRIPE_SECRET'),
                    string(credentialsId: 'STRIPE_PUBLISHABLE_KEY', variable: 'STRIPE_PUBLISHABLE'),
                    string(credentialsId: 'STRIPE_WEBHOOK_SECRET',  variable: 'STRIPE_WEBHOOK')
                ]) {
                    sh '''
                        docker stop ${CONTAINER_NAME} || true
                        docker rm   ${CONTAINER_NAME} || true
                        docker run -d \
                            --name ${CONTAINER_NAME} \
                            --restart unless-stopped \
                            -p 8082:8082 \
                            -e ASPNETCORE_ENVIRONMENT=Production \
                            -e Stripe__SecretKey="${STRIPE_SECRET}" \
                            -e Stripe__PublishableKey="${STRIPE_PUBLISHABLE}" \
                            -e Stripe__WebhookSecret="${STRIPE_WEBHOOK}" \
                            ${IMAGE_NAME}
                    '''
                }
            }
        }
    }

    post {
        failure {
            echo 'Pipeline failed.'
        }
        success {
            echo 'Deploy successful.'
        }
    }
}
