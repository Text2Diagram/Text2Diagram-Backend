pipeline {
    agent any

    environment {
        DEPLOY_DIR = "/home/kuro/Text2Diagram-Backend"
        REMOTE_HOST = "103.169.35.220"
        REMOTE_USER = "kuro"
    }

    stages {
        stage('Clone') {
            steps {
                git 'https://github.com/Text2Diagram/Text2Diagram-Backend.git'
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet publish ./Text2Diagram-Backend/Text2Diagram-Backend.csproj -c Release -o out'
            }
        }

        stage('Deploy') {
            steps {
                sshagent(credentials: ['8c3736d6-fe8e-4b63-9bb3-faf29895682e']) {
                    sh """
                    ssh -o StrictHostKeyChecking=no $REMOTE_USER@$REMOTE_HOST 'rm -rf ~/Text2Diagram-Backend/out'
                    scp -r ./Text2Diagram-Backend/out $REMOTE_USER@$REMOTE_HOST:~/Text2Diagram-Backend/
                    ssh $REMOTE_USER@$REMOTE_HOST 'sudo systemctl restart text2diagram'
                    """
                }
            }
        }
    }
}
