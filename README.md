<img width="700" height="700" alt="image" src="https://github.com/user-attachments/assets/954bf37d-d7a5-4f94-9c15-cb7847125bfd" />


# URL Shortener API

Sistema backend de encurtamento de URLs com cache Redis, métricas via Prometheus e dashboards no Grafana.

## Funcionalidades
- Criar, consultar e deletar URLs curtas via API REST.
- Persistência em PostgreSQL.
- Cache Redis para otimização de performance.
- Contabilização de hits nos redirecionamentos.
- Métricas customizadas via Prometheus.
- Dashboards no Grafana para monitoramento de uso, cache e performance.
- Suporte a Docker e docker-compose para levantar todo o ambiente com um comando.

## Tecnologias
- .NET 8 / ASP.NET Core
- PostgreSQL
- Redis
- Prometheus
- Grafana
- Serilog (logging)
- Docker

## Endpoints Principais

| Método | Rota | Descrição |
|--------|------|-----------|
| POST   | /api/urls | Cria uma URL curta |
| GET    | /:shortKey | Redireciona para a URL original |
| GET    | /api/urls/:shortKey | Retorna metadados da URL curta (original, hits, datas) |
| DELETE | /api/urls/:shortKey | Remove a URL curta |

## Configuração e Execução

## Requisitos

Antes de rodar o projeto, certifique-se de ter os seguintes softwares e serviços instalados/configurados:

- **Docker** - para containerizar a API, banco de dados, Redis, Prometheus e Grafana.
- **Docker Compose** - para subir todos os serviços com um único comando.
- **PostgreSQL** - banco de dados relacional para persistência das URLs.
- **Redis** - cache para otimização de consultas e armazenamento de hits temporários.
- **Prometheus** - coleta de métricas da aplicação.
- **Grafana** - dashboards para visualização das métricas e monitoramento.
- **.NET 8 SDK** - necessário caso queira rodar a aplicação localmente fora do Docker.

### Variáveis de Ambiente

Crie um arquivo `.env` na raiz com suas variáveis de ambiente (exemplo `.env.example`):

```env
POSTGRES_DB=urlshortener
POSTGRES_USER=postgres
POSTGRES_PASSWORD=sa123
REDIS_CONNECTION=redis:6379
```
### Subir Serviços com Docker

Certifique-se de estar na raiz da solução (onde está localizado o `docker-compose.yml`) e execute:

```bash
docker-compose up --build
```


## Acessar Serviços

- **API:** [http://localhost:5000](http://localhost:5000)
- **Grafana:** [http://localhost:3000](http://localhost:3000)  
  - Usuário/admin: `admin/admin`
- **Prometheus:** [http://localhost:9090](http://localhost:9090)

## Observabilidade

<img width="1619" height="651" alt="image" src="https://github.com/user-attachments/assets/8cd359fe-e582-44b2-8e5e-82d02dc3db86" />

<img width="1908" height="419" alt="image" src="https://github.com/user-attachments/assets/b036848e-5a3c-48c1-8a4e-bbcdfe60c99a" />


- Top 10 URLs mais acessadas
- Hits por URL ao longo do tempo
- Taxa de cache hits/misses
