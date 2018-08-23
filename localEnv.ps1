docker kill $(docker ps -q)
docker-compose -p ODataIntegrationTests -f docker-compose.yml up -d