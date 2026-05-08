# Docker Setup Guide

This document explains how to run the Reservation Management API using Docker Engine.

## Prerequisites

- Docker Engine (not Docker Desktop)
- Docker Compose (v2.0+)
- 4GB RAM minimum
- 10GB free disk space

## Quick Start

### 1. Setup Environment Variables

Copy the example environment file and update it with your values:

```bash
cp .env.example .env
```

Edit `.env` and set secure values for:
- `JWT_SECRET_KEY` - Your JWT secret (minimum 32 characters recommended)
- `DB_PASSWORD` - Your database password

### 2. Build and Start Services

```bash
# Build the API image
docker-compose build

# Start all services (development mode)
docker-compose up -d

# Verify services are running
docker-compose ps
```

### 3. Access the API

- **API**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **Health Check**: http://localhost:8080/health

### 4. View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f reservation-api
docker-compose logs -f postgres
docker-compose logs -f kafka
```

## Service Architecture

```
┌─────────────────────────────────────────────┐
│         Docker Network: reservation_network  │
├─────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐        │
│  │ Reservation  │  │  PostgreSQL  │        │
│  │     API      ├──┤   (5432)     │        │
│  │  (8080)      │  │              │        │
│  └──────────────┘  └──────────────┘        │
└─────────────────────────────────────────────┘
```

## Development Workflow

### Running Services

```bash
# Start all services
docker-compose up

# Start in background
docker-compose up -d

# Stop services
docker-compose down

# Remove volumes (clear database)
docker-compose down -v

# Rebuild after code changes
docker-compose build --no-cache
docker-compose up
```



### Database Access

Access PostgreSQL directly:

```bash
# Enter PostgreSQL container
docker-compose exec postgres psql -U postgres -d ReservationManagement

# Common commands
\dt                    # List tables
\c ReservationManagement # Connect to database
SELECT * FROM "Reservations";
```

## Production Deployment

### Using Production Override

```bash
# Copy environment file
cp .env.example .env

# Update .env with production values (IMPORTANT!)
nano .env

# Start with production configuration
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Security Considerations

1. **Change Default Passwords**
   - `JWT_SECRET_KEY`: Use a strong, random key (minimum 64 characters)
   - `DB_PASSWORD`: Use a strong, random password

2. **Use Environment Variables**
   - Never hardcode secrets in compose file
   - Use `.env` file (add to `.gitignore`)
   - Or use Docker Secrets (for Swarm) or environment variables

3. **Network Isolation**
   - Services communicate over internal Docker network
   - Only expose necessary ports
   - Use firewall rules on the host

4. **Database Backups**
   ```bash
   # Backup database
   docker-compose exec postgres pg_dump -U postgres ReservationManagement > backup.sql
   
   # Restore database
   docker-compose exec -T postgres psql -U postgres ReservationManagement < backup.sql
   ```

5. **Log Management**
   - Monitor `/app/logs` directory
   - Implement log rotation
   - Consider centralized logging (ELK stack, Splunk, etc.)

## Troubleshooting

### Container Won't Start

```bash
# Check logs
docker-compose logs reservation-api

# Rebuild the image
docker-compose build --no-cache reservation-api
```

### Database Connection Issues

```bash
# Verify PostgreSQL is running
docker-compose ps postgres

# Check database logs
docker-compose logs postgres

# Verify network connectivity
docker-compose exec reservation-api ping postgres
```

### Port Already in Use

```bash
# Find what's using the port (on Linux)
sudo lsof -i :8080

# Kill the process
sudo kill -9 <PID>

# Or change the port in docker-compose
# Edit the ports section: "9080:8080"
```

### Insufficient Disk Space

```bash
# Clean up unused images and volumes
docker system prune -a

# Remove all stopped containers
docker container prune

# Remove unused volumes
docker volume prune
```

## Performance Tuning

### Docker Engine Settings

For optimal performance, ensure adequate resources:

```bash
# Check current limits
docker info

# On Linux, adjust memory if needed:
# Edit /etc/docker/daemon.json
{
  "storage-driver": "overlay2",
  "insecure-registries": [],
  "memory": 4294967296,
  "memswap": 4294967296
}
```

### Container Resource Limits

Edit `docker-compose.prod.yml`:

```yaml
services:
  reservation-api:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 1024M
        reservations:
          cpus: '1'
          memory: 512M
```

### Database Optimization

```bash
# Connect to PostgreSQL
docker-compose exec postgres psql -U postgres -d ReservationManagement

# Analyze queries
EXPLAIN ANALYZE SELECT * FROM "Reservations";

# Create indexes
CREATE INDEX idx_reservation_user_id ON "Reservations"("UserId");
```

## Advanced Configuration

### Monitoring with Prometheus

Add monitoring service:

```yaml
prometheus:
  image: prom/prometheus:latest
  volumes:
    - ./prometheus.yml:/etc/prometheus/prometheus.yml
  ports:
    - "9090:9090"
```

### SSL/TLS Configuration

For production, enable SSL:

1. Generate certificates
2. Update Kafka configuration
3. Update connection strings

## Useful Commands

```bash
# View running services
docker-compose ps

# Stop all services
docker-compose stop

# Restart a service
docker-compose restart reservation-api

# Execute command in container
docker-compose exec reservation-api ls -la /app

# View resource usage
docker stats

# Build and test
docker-compose build && docker-compose up --abort-on-container-exit

# Clean everything (WARNING: deletes data)
docker-compose down -v --remove-orphans
```

## Health Checks

All services include health checks:

```bash
# View health status
docker-compose ps

# Inspect health
docker inspect --format='{{.State.Health.Status}}' reservation-api
```

## Support

For issues or questions:
- Check logs: `docker-compose logs [service-name]`
- Review Docker Compose documentation
- Check service-specific documentation
