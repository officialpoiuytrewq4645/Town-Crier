FROM ubuntu

RUN apt-get update && apt-get install openssl1.0 curl -y

WORKDIR /data
COPY build /data
RUN chmod -R 777 .

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="true"
ENV USE_ENV_ALTA_LOGIN="true"

CMD ["/data/Town-Crier"]