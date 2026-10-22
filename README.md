# CorpHome

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/aiursoftweb/corpHome/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.com/aiursoft/corpHome/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/corpHome/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.com/aiursoft/corpHome/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/corpHome/-/pipelines)
[![S360 compliance](https://s360.aiursoft.com/badge.svg?project=aiursoft%2Fcorphome)](https://s360.aiursoft.com/SupplyChain?tab=projects&project=aiursoft%2Fcorphome&scope=baseline&ecosystem=all)
[![Man hours](https://manhours.aiursoft.com/r/github.com/aiursoftweb/corpHome.svg)](https://manhours.aiursoft.com/r/github.com/aiursoftweb/corpHome.html)
[![Website](https://img.shields.io/website?url=https%3A%2F%2Fwww.aiursoft.com)](https://www.aiursoft.com)
[![Docker](https://img.shields.io/docker/pulls/aiursoft/corphome.svg)](https://hub.docker.com/r/aiursoft/corphome)

CorpHome is Aiursoft's official website homepage.

![screenshot](./screenshot.png)

Default user name is `admin@default.com` and default password is `Admin@123456!`.

## Try

Try a running CorpHome [here](https://corpHome.aiursoft.com).

## Install with Docker

Run the installer with the desired port (omit it to choose an available port):

```bash
curl -fsSL https://raw.githubusercontent.com/aiursoftweb/corphome/master/install.sh | sudo bash -s -- 8080
```

Installation starts after three seconds; press Ctrl+C to cancel. Then open `http://localhost:8080`.

## Run manually

Requirements about how to run

1. Install [.NET 10 SDK](http://dot.net/) and [Node.js](https://nodejs.org/).
2. Execute `npm install` at `wwwroot` folder to install the dependencies.
3. Execute `dotnet run` to run the app.
4. Use your browser to view [http://localhost:5000](http://localhost:5000).

## Run in Microsoft Visual Studio

1. Open the `.sln` file in the project path.
2. Press `F5` to run the app.

## Run in Docker

First, install Docker [here](https://docs.docker.com/get-docker/).

Then run the following commands in a Linux shell:

```bash
image=aiursoft/corpHome
appName=corpHome
sudo docker pull $image
sudo docker run -d --name $appName --restart unless-stopped -p 5000:5000 -v /var/www/$appName:/data $image
```

That will start a web server at `http://localhost:5000` and you can test the app.

The docker image has the following context:

| Properties  | Value                           |
|-------------|---------------------------------|
| Image       | aiursoft/corpHome               |
| Ports       | 5000                            |
| Binary path | /app                            |
| Data path   | /data                           |
| Config path | /data/appsettings.json          |

## Contact alerts in AlertCenter

New valid contact submissions are saved with a stable report ID in the same database write as the contact. A background job sends one **sev2** incident per contact, normally within one minute, using `Aiursoft.AlertCenter.SDK`. Contact submissions never wait for AlertCenter. Existing contacts from before this integration have no report ID and are not backfilled.

Configure the following global settings in the administration UI or override them with environment variables:

| Environment variable | Default | Purpose |
|---|---|---|
| `GlobalSettings__AlertCenter_Enabled` | `False` | Enable delivery. Disabling pauses delivery but retains new submissions for later delivery. |
| `GlobalSettings__AlertCenter_Endpoint` | `https://alertcenter.aiursoft.com` | AlertCenter HTTP(S) base URL. |
| `GlobalSettings__AlertCenter_ApiKey` | Empty | Dedicated CorpHome application key with **create incident** permission. Stored as a secret when configured in the UI. |
| `GlobalSettings__AlertCenter_PublicBaseUrl` | `https://www.aiursoft.com` | CorpHome HTTP(S) base URL for authorized contact-list links. |

SQLite and MySQL migrations add nullable report ID and delivery timestamp columns; they are applied at application startup. Failed deliveries remain pending across restarts and are retried by the next job. The same report ID is reused after timeouts or local receipt-save failures, so AlertCenter's application-scoped idempotency prevents duplicate incidents. Keep the same AlertCenter application and its incident history when rotating API keys. The job processes submissions sequentially in pages of 100 and continues with other contacts after an individual failure; failures are visible on the Jobs page and in logs.

Incidents contain only the contact ID, submission time and a link to `/Contacts#contact-{id}`; names, email addresses and inquiry text stay in CorpHome. Responders need CorpHome contact-view permission. Contact follow-up, acknowledgement and resolution remain manual; successful notification does not automatically mitigate the inquiry.

Before enabling production delivery, create the CorpHome application/key in AlertCenter, configure the sev2 notification route and responder access, and supply `CORPHOME_ALERTCENTER_API_KEY` to the Box deployment. Deploy CorpHome before applying the Box configuration. Verify a valid test submission produces exactly one incident and reaches the intended channel; invalid submissions must not produce incidents. Do not use a production endpoint or key for automated tests.

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you with push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.
