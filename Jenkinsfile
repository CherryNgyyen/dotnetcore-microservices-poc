pipeline {
    agent {
        kubernetes {
            yaml """
apiVersion: v1
kind: Pod
metadata:
  labels:
    app: dotnet-build
spec:
  containers:
    - name: dotnet
      image: mcr.microsoft.com/dotnet/sdk:8.0
      command:
        - cat
      tty: true
"""
        }
    }

    environment {
        JAVA_HOME = '/usr/lib/jvm/java-17-openjdk-amd64'
        PATH = "$JAVA_HOME/bin:$PATH"
        SONARQUBE_SERVER = 'http://sonarqube-service:9000/sonarqube/'
        SONARQUBE_TOKEN = credentials('sonarqube-user-token')
        SONARQUBE_PROJECT_KEY = credentials('sonarqube-microservice-solution-project')
        DOTNET_VERSION = '8.0'
        SONARQUBE_PROJECT_NAME = "dotnetcore-microservices-poc-${env.BRANCH_NAME ?: 'master'}"
    }

    options {
        buildDiscarder(logRotator(
            numToKeepStr: '5',
            artifactDaysToKeepStr: '10'
        ))
    }

    stages {
        stage('Install Java') {
            steps {
                container('dotnet') {
                    script {
                        echo 'Installing OpenJDK 17...'
                        sh '''
                        apt-get update -y
                        apt-get install -y openjdk-17-jdk
                        '''
                        echo 'Verifying Java installation...'
                        sh 'java -version'
                    }
                }
            }
        }

        stage('Checkout') {
            steps {
                container('dotnet') {
                    checkout scm
                }
            }
        }

        stage('Set Build Name') {
            steps {
                script {
                    def timestamp = new Date().format("yyyy.MM.dd.HHmm")
                    currentBuild.displayName = "${timestamp}"
                }
            }
        }

        stage('Restore Dependencies') {
            steps {
                container('dotnet') {
                    script {
                        echo 'Restoring dependencies...'
                        sh 'dotnet restore DotNetMicroservicesPoc.sln'
                    }
                }
            }
        }

        stage('Build Solution') {
            steps {
                container('dotnet') {
                    script {
                        echo 'Building the solution...'
                        sh 'dotnet build DotNetMicroservicesPoc.sln --configuration Release'
                    }
                }
            }
        }

        stage('Run Tests') {
            steps {
                container('dotnet') {
                    script {
                        echo 'Running tests...'

                        sh 'dotnet test PricingService.Test/PricingService.Test.csproj --configuration Release --no-build --logger "trx;LogFileName=PricingService_TestResults.trx" --verbosity detailed'
                        // sh 'dotnet test PolicyService.Test/PolicyService.Test.csproj --configuration Release --no-build --logger "trx;LogFileName=PolicyService_TestResults.trx" --verbosity detailed'
                        sh 'dotnet test PaymentService.Test/PaymentService.Test.csproj --configuration Release --no-build --logger "trx;LogFileName=PaymentService_TestResults.trx" --verbosity detailed'

                        xunit([
                            MSTest(deleteOutputFiles: true, failIfNotNew: true, pattern: '**/PricingService_TestResults.trx', skipNoTestFiles: false, stopProcessingIfError: true),
                            // MSTest(deleteOutputFiles: true, failIfNotNew: true, pattern: '**/PolicyService_TestResults.trx', skipNoTestFiles: false, stopProcessingIfError: true),
                            MSTest(deleteOutputFiles: true, failIfNotNew: true, pattern: '**/PaymentService_TestResults.trx', skipNoTestFiles: false, stopProcessingIfError: true)
                        ])
                        archiveArtifacts artifacts: '**/PricingService_TestResults.trx'
                        archiveArtifacts artifacts: '**/PaymentService_TestResults.trx'
                    }
                }
            }
        }

        stage('SonarQube Analysis') {
            steps {
                container('dotnet') {
                    script {
                        echo "Starting SonarQube analysis for project: ${env.SONARQUBE_PROJECT_NAME}..."
                        withSonarQubeEnv('SonarQube') {
                            sh '''
                            dotnet tool install --global dotnet-sonarscanner
                            export PATH="$PATH:/root/.dotnet/tools"

                            dotnet sonarscanner begin /k:"$SONARQUBE_PROJECT_KEY" /d:sonar.host.url="$SONARQUBE_SERVER" /d:sonar.login="$SONARQUBE_TOKEN" /n:"$SONARQUBE_PROJECT_NAME"
                            dotnet build DotNetMicroservicesPoc.sln
                            dotnet sonarscanner end /d:sonar.login="$SONARQUBE_TOKEN"
                            '''
                        }
                        timeout(time: 60, unit: 'MINUTES') {
                            waitForQualityGate abortPipeline: true
                        }
                    }
                }
            }
        }

        stage('Publish') {
            steps {
                container('dotnet') {
                    script {
                        echo 'Publishing solution...'
                        sh 'dotnet publish DotNetMicroservicesPoc.sln --configuration Release --no-build'
                    }
                }
            }
        }
    }

    post {
        always {
            cleanWs()
        }
    }
}
