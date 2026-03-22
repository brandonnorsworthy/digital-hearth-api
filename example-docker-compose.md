services:
  discord-status:
    build: .
    container_name: discord-status
    env_file:
      - .env
    ports:
      - "3500:3500"
    volumes:
      - ./data:/app/data
    restart: unless-stopped
    stop_grace_period: 90s
    mem_limit: 512m
    cpus: 1
