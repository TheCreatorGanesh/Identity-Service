FROM postgres:16-alpine@sha256:f2f98726820d0342055351c6e9d36e705f5bfffb3dd53ff556325114b08afb6d

ENV POSTGRES_USER=${POSTGRES_USER:-postgres}
ENV POSTGRES_PASSWORD=${POSTGRES_PASSWORD:-postgres}
ENV POSTGRES_DB=${POSTGRES_DB:-IdentityManagement}

COPY infra/database/init.sql /docker-entrypoint-initdb.d/

VOLUME ["/var/lib/postgresql/data"]

EXPOSE 5432
