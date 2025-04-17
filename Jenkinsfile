pipeline {
    agent {
        docker {
            image 'mcr.microsoft.com/dotnet/sdk:8.0'
            args '-v /home/jenkins/.ssh:/root/.ssh' // nếu bạn cần SSH
        }
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
    }
}
