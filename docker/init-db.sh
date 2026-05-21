#!/bin/bash
set -e

/opt/mssql/bin/sqlservr &
SQLPID=$!

echo "Chờ SQL Server khởi động..."
for i in $(seq 1 30); do
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" -C > /dev/null 2>&1 && break
    sleep 2
done

echo "Chạy script khởi tạo database..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -i /docker/init.sql -C
echo "Database đã sẵn sàng!"

wait $SQLPID
