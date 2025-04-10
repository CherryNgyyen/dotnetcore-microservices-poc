pipeline {
    agent {
        docker {
            image 'mcr.microsoft.com/dotnet/sdk:8.0'
            args '--network devops_tools_stack_service_network -v /tmp:/tmp'
        }
    }

    environment {
        JAVA_HOME = '/usr/lib/jvm/java-17-openjdk-amd64'
        PATH = "$JAVA_HOME/bin:$PATH"
        SONARQUBE_SERVER = 'http://sonarqube:9000/sonarqube/'
        SONARQUBE_TOKEN = credentials('sonarqube-user-token')
        SONARQUBE_PROJECT_KEY = credentials('sonarqube-microservice-solution-project')
        DOTNET_VERSION = '8.0'
        SONARQUBE_PROJECT_NAME = "dotnetcore-microservices-poc-${env.BRANCH_NAME ?: 'master'}"
    }

    stages {
        stage('Install Java') {
            steps {
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

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Restore Dependencies') {
            steps {
                script {
                    echo 'Restoring dependencies...'
                    sh 'dotnet restore DotNetMicroservicesPoc.sln'
                }
            }
        }

        stage('Build Solution') {
            steps {
                script {
                    echo 'Building the solution...'
                    sh 'dotnet build DotNetMicroservicesPoc.sln --configuration Release'
                }
            }
        }

        stage('Run Tests') {
            steps {
                script {
                    echo 'Running tests...'

                    // Run tests for each project separately and save results in unique trx files
                    sh 'dotnet test PricingService.Test/PricingService.Test.csproj --configuration Release --no-build --logger "trx;LogFileName=PricingService_TestResults.trx" --verbosity detailed'
                    // sh 'dotnet test PolicyService.Test/PolicyService.Test.csproj --configuration Release --no-build --logger "trx;LogFileName=PolicyService_TestResults.trx" --verbosity detailed'
                    sh 'dotnet test PaymentService.Test/PaymentService.Test.csproj --configuration Release --no-build --logger "trx;LogFileName=PaymentService_TestResults.trx" --verbosity detailed'

                    // Publish the test results for each project using xUnit Plugin
                    xunit([
                        MSTest(deleteOutputFiles: true, failIfNotNew: true, pattern: '**/PricingService_TestResults.trx', skipNoTestFiles: false, stopProcessingIfError: true),
                        // MSTest(deleteOutputFiles: true, failIfNotNew: true, pattern: '**/PolicyService_TestResults.trx', skipNoTestFiles: false, stopProcessingIfError: true),
                        MSTest(deleteOutputFiles: true, failIfNotNew: true, pattern: '**/PaymentService_TestResults.trx', skipNoTestFiles: false, stopProcessingIfError: true)
                    ])
                }
            }
        }

        stage('SonarQube Analysis') {
            steps {
                script {
                    echo "Starting SonarQube analysis for project: ${env.SONARQUBE_PROJECT_NAME}..."
                    withSonarQubeEnv('SonarQube') {
                        sh '''
                        # Install SonarScanner for .NET (if not already installed)
                        dotnet tool install --global dotnet-sonarscanner
                        
                        # Add .NET Core SDK tools to PATH
                        export PATH="$PATH:/root/.dotnet/tools"
                        
                        # Begin SonarQube analysis with dynamic project name
                        dotnet sonarscanner begin /k:"$SONARQUBE_PROJECT_KEY" /d:sonar.host.url="$SONARQUBE_SERVER" /d:sonar.login="$SONARQUBE_TOKEN" /n:"$SONARQUBE_PROJECT_NAME"
                        
                        # Build the solution to trigger analysis
                        dotnet build DotNetMicroservicesPoc.sln
                        
                        # End SonarQube analysis
                        dotnet sonarscanner end /d:sonar.login="$SONARQUBE_TOKEN"
                        '''
                    }
                }
            }
        }

        stage('Publish') {
            steps {
                script {
                    echo 'Publishing solution...'
                    sh 'dotnet publish DotNetMicroservicesPoc.sln --configuration Release --no-build'
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
