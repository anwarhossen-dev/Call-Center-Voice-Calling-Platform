# Step-by-Step Development Guide & Implementation Roadmap
## In-House Call Center Voice Calling Platform

**Project:** Enterprise Call Center Voice Calling Platform  
**Target Scale:** 50+ Active Agents (MVP) scaling to 500+ Agents  
**Telecom Carrier:** BTCL (Bangladesh Telecommunications Company Limited)  
**Technology Stack:** FreeSWITCH (Telephony), .NET Core (Backend), Angular (Frontend), Redis, RabbitMQ, SQL Server, MinIO  

---

## Architecture Flow Overview

```
[BTCL SIP Trunk] 
       │ (G.711 / SIP)
       ▼
[Kamailio / FreeSWITCH Core] ◄════ (WSS / WebRTC) ════► [Angular Browser Softphone]
       │ (ESL Socket Events)
       ▼
[.NET Core Backend Services] ◄──── (SignalR WebSockets) ──► [Agent & Supervisor UI]
   ├── Redis (Agent Presence & Queue Locks)
   ├── RabbitMQ (Async Events & Recording Offload)
   ├── SQL Server (CDRs, Users, Queues, Dispositions)
   └── MinIO Object Storage (Encrypted Audio Recordings)
```

---

# Phase 0: Infrastructure, Network & Telecom Trunk Setup
**Goal:** Establish physical carrier connectivity, virtual machines, and network security foundations.

### Step 0.1: BTCL SIP Trunk Provisioning
1. **Physical Hand-off:** Terminate the BTCL leased line / Metro Ethernet cable into your datacenter rack switch or dedicated router.
2. **IP Assignment:** Configure the network interface with BTCL-provided parameters:
   - Carrier Gateway IP
   - Local Subnet Mask & Static IP
   - Primary and Secondary Carrier DNS / SIP Registrar IPs
3. **Firewall & Routing:**
   - Create a dedicated VoIP VLAN (e.g., VLAN 100) isolating voice traffic from corporate office traffic.
   - Allow UDP/TCP port `5060` (SIP Signaling) to/from BTCL Gateway IP only.
   - Allow UDP ports `10000-20000` (RTP Voice Media Streams).

### Step 0.2: Host Environment & Server Allocation
Deploy Linux servers (Ubuntu 22.04 LTS or Rocky Linux 9):
- **Server 1 (Telephony Core):** 8 Cores, 16 GB RAM, High-speed NVMe SSD (FreeSWITCH & Coturn STUN/TURN).
- **Server 2 (Application & Middleware):** 8 Cores, 16 GB RAM (.NET Core APIs, Redis, RabbitMQ, MinIO).
- **Server 3 (Database Tier):** 8 Cores, 32 GB RAM (Microsoft SQL Server Linux or Windows Server).

### Step 0.3: Domain, SSL & WebRTC Certificates
WebRTC **strictly requires HTTPS and WSS (Secure WebSockets)** in modern browsers:
- Procure internal/external domain names: `telephony.company.com` and `cc-api.company.com`.
- Generate valid SSL/TLS certificates (Let's Encrypt or Corporate Enterprise CA).

---

# Phase 1: Telephony Core & WebRTC Gateway Setup
**Goal:** Install FreeSWITCH, connect it to the BTCL SIP trunk, and configure secure WebSockets for browser calling.

### Step 1.1: FreeSWITCH Installation
Install FreeSWITCH v1.10+ with core modules enabled:
- `mod_sofia` (SIP engine)
- `mod_event_socket` (Remote control API)
- `mod_dptools` (Dialplan tools)
- `mod_opus` (WebRTC audio codec)
- `mod_sndfile` & `mod_shout` (Audio recording and playback)
- `mod_rtc` & `mod_verto` (WebRTC media switching)

### Step 1.2: BTCL SIP Trunk Gateway Configuration
Create gateway configuration in `/etc/freeswitch/directory/external/btcl_trunk.xml`:
```xml
<include>
  <gateway name="btcl_trunk">
    <!-- BTCL Carrier SIP Server IP -->
    <param name="realm" value="192.168.10.1"/>
    <param name="proxy" value="192.168.10.1"/>
    <param name="register" value="false"/> <!-- Set to true if BTCL requires SIP registration -->
    <param name="caller-id-in-from" value="true"/>
    <param name="ping" value="25"/>
    <param name="channels-media-options" value="nomedia"/>
    <param name="codec-prefs" value="PCMA,PCMU"/>
  </gateway>
</include>
```

### Step 1.3: WebRTC Profile Configuration (WSS on Port 7443)
In `/etc/freeswitch/sip_profiles/internal.xml`, configure the secure WebRTC interface:
```xml
<param name="ws-binding" value=":5066"/>
<param name="wss-binding" value=":7443"/>
<param name="tls" value="true"/>
<param name="tls-cert-dir" value="/etc/freeswitch/tls"/>
<param name="tls-version" value="tlsv1.2,tlsv1.3"/>
<param name="apply-candidate-acl" value="localnet.auto"/>
<param name="inbound-codec-prefs" value="OPUS,PCMA,PCMU"/>
```

### Step 1.4: Inbound Dialplan & Audio Recording Setup
Configure `/etc/freeswitch/dialplan/public/00_inbound_btcl.xml` to capture inbound calls and initiate dual-channel recording:
```xml
<include>
  <extension name="BTCL_Inbound_Queue">
    <condition field="destination_number" expression="^(029876543|09612345678)$">
      <action application="set" data="RECORD_STEREO=true"/>
      <action application="set" data="media_bug_answer_req=true"/>
      <!-- Left channel: Agent, Right channel: Customer -->
      <action application="record_session" data="/recordings/raw/${uuid}.wav"/>
      <!-- Hand off call control to .NET Core Event Socket Listener -->
      <action application="park"/>
    </condition>
  </extension>
</include>
```

---

# Phase 2: Database & Messaging Layer Setup
**Goal:** Initialize the relational persistence, caching, and asynchronous event infrastructure.

### Step 2.1: Relational Database Initialization (SQL Server)
1. Initialize the SQL Server instance.
2. Execute migration scripts creating the core tables:
   - `Users` & `Roles` (Authentication & RBAC)
   - `Agents` (Agent profile, extension, skills, active state)
   - `AgentStateLogs` (Audit history of every status change)
   - `Queues` & `AgentQueues` (ACD routing strategies)
   - `Calls` (Central Call Detail Record - CDR)
   - `CallSessions` (Leg A/Leg B timing & disconnect causes)
   - `CallRecordings` (Audio file metadata, duration, MinIO S3 URI)
   - `Dispositions` (Wrap-up codes: Interested, Escalated, Callback, Resolved)

### Step 2.2: Redis Caching & In-Memory State Setup
Configure Redis for sub-millisecond agent state tracking:
- **Agent State Hash:** `agent:state:{AgentId}` -> stores `Status`, `Since`, `Extension`, `CallUuid`.
- **Queue Available Sorted Set:** `queue:avail:{QueueId}` -> Member is `AgentId`, Score is `EpochTimestamp` of when the agent became available. (Enables $O(1)$ lookup for the longest-idle agent).

### Step 2.3: RabbitMQ Event Broker Setup
Configure RabbitMQ Exchanges and Queues:
- Exchange: `callcenter.events` (Topic Exchange)
  - Routing Key `call.started` -> Queue: `cdr.processor`
  - Routing Key `call.ended` -> Queue: `recording.offloader`
  - Routing Key `call.disposed` -> Queue: `crm.sync`
  - Routing Key `call.audio.ready` -> Queue: `ai.transcription`

### Step 2.4: MinIO S3-Compatible Object Storage Setup
1. Deploy MinIO container on Linux.
2. Create private bucket: `call-center-recordings`.
3. Configure bucket policy: Private access only via S3 access keys or pre-signed URLs.

---

# Phase 3: Backend Services Development (.NET Core)
**Goal:** Build the business logic, telephony event socket listeners, queue routing engine, and real-time SignalR hubs.

### Step 3.1: Clean Architecture Solution Structure
```
CallCenter.Platform/
├── src/
│   ├── Core/
│   │   ├── CallCenter.Domain/            # Entities, Enums, Domain Events
│   │   └── CallCenter.Application/       # Use Cases, CQRS Handlers, Interfaces
│   ├── Infrastructure/
│   │   ├── CallCenter.Persistence/       # EF Core, Dapper, SQL Server Repositories
│   │   ├── CallCenter.Telephony.ESL/     # FreeSWITCH Event Socket Connection
│   │   ├── CallCenter.Messaging/         # RabbitMQ Producers & Consumers
│   │   └── CallCenter.Storage/           # MinIO S3 SDK Client
│   └── Presentation/
│       ├── CallCenter.WebApi/            # REST Controllers, Auth JWT, Swagger
│       ├── CallCenter.Realtime/          # ASP.NET Core SignalR Hubs
│       └── CallCenter.WorkerService/     # Background Recording Offloader Daemon
```

### Step 3.2: FreeSWITCH ESL Listener Implementation
Implement a background service using TCP sockets to communicate with FreeSWITCH on port `8021`:
```csharp
public class FreeSwitchEventListener : BackgroundService
{
    private readonly ILogger<FreeSwitchEventListener> _logger;
    private readonly IServiceProvider _serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Connect to FreeSWITCH Event Socket
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 8021, stoppingToken);
        using var networkStream = client.GetStream();
        using var reader = new StreamReader(networkStream);
        using var writer = new StreamWriter(networkStream) { AutoFlush = true };

        // Authenticate with FreeSWITCH ESL
        await writer.WriteLineAsync("auth ClueCon");
        // Subscribe to relevant call events
        await writer.WriteLineAsync("events json CHANNEL_CREATE CHANNEL_ANSWER CHANNEL_HANGUP RECORD_STOP");

        while (!stoppingToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line != null && line.Contains("RECORD_STOP"))
            {
                // Parse Event JSON and publish to RabbitMQ
                await HandleRecordStopEvent(line);
            }
        }
    }
}
```

### Step 3.3: Automatic Call Distribution (ACD) Routing Engine
When an inbound call event arrives:
1. Fetch caller ID from event payload.
2. Check queue rules: Find target `QueueId`.
3. Execute Redis command:
   ```bash
   ZRANGE queue:avail:{QueueId} 0 0
   ```
   *(Picks the agent who has been idle the longest).*
4. Lock agent status in Redis: Set to `Reserved`.
5. Instruct FreeSWITCH via ESL:
   ```bash
   api originate sofia/internal/1042%telephony.company.com &bridge(uuid-of-inbound-call)
   ```
6. Push Screen-Pop data to Agent 1042 via SignalR.

### Step 3.4: SignalR Real-Time Notification Hubs
Expose WebSocket hubs for Angular:
- `CallHub`: Sends `ReceiveCallAlert`, `CallConnected`, `CallEnded`.
- `SupervisorHub`: Broadcasts `AgentStateChanged`, `QueueStatsUpdated`.

---

# Phase 4: CRM Integration Module
**Goal:** Automatically match incoming callers with customer records and synchronize post-call details back into the CRM.

### Step 4.1: CRM Lookup & Screen-Pop Service
```csharp
public class CrmIntegrationService : ICrmIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;

    public async Task<CustomerProfileDto> LookupCustomerAsync(string phoneNumber)
    {
        // 1. Check local cache (fast return < 10ms)
        var cached = await _cache.GetStringAsync($"crm:customer:{phoneNumber}");
        if (cached != null) return JsonSerializer.Deserialize<CustomerProfileDto>(cached);

        // 2. Query CRM REST API (with 1.5s timeout fallback)
        var response = await _httpClient.GetAsync($"/api/v1/customers/by-phone?phone={phoneNumber}");
        if (!response.IsSuccessStatusCode)
            return CustomerProfileDto.Anonymous(phoneNumber);

        var profile = await response.Content.ReadFromJsonAsync<CustomerProfileDto>();
        await _cache.SetStringAsync($"crm:customer:{phoneNumber}", 
            JsonSerializer.Serialize(profile), 
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

        return profile;
    }
}
```

### Step 4.2: Asynchronous Post-Call Sync Worker
1. When an agent submits call notes and disposition, publish `CallDisposedEvent` to RabbitMQ.
2. Background worker consumes the event and issues a `POST /api/v1/activities/call-log` to the CRM with:
   - Customer ID
   - Call Start, Answer, End Times
   - Duration (Talk Time, Hold Time)
   - Agent Name & Extension
   - Disposition Category & Notes
   - Secure playback link to recording

---

# Phase 5: Frontend Development (Angular)
**Goal:** Build responsive, zero-install WebRTC softphone portals for Agents, Supervisors, and Administrators.

### Step 5.1: Angular Application Structure
```
src/app/
├── core/
│   ├── services/
│   │   ├── webrtc-sip.service.ts     # SIP.js / JsSIP WebRTC wrapper
│   │   ├── signalr.service.ts        # SignalR event listeners
│   │   ├── agent-state.service.ts    # State machine (Available, ACW, Break)
│   │   └── audio-player.service.ts   # Audio playback controller
├── features/
│   ├── agent-portal/
│   │   ├── softphone-bar/            # Answer, Hangup, Hold, Mute, Transfer, DTMF
│   │   ├── screen-pop/               # CRM customer detail card
│   │   └── wrap-up-modal/            # Disposition tagging & notes
│   ├── supervisor-portal/
│   │   ├── live-agent-grid/          # Status tiles with live timers
│   │   ├── queue-monitor/            # Calls waiting, SLA percentage
│   │   └── coaching-controls/        # Spy, Whisper, Barge-in buttons
│   └── admin-portal/
│       ├── agent-management/         # Create users, assign queues & skills
│       ├── recording-browser/        # Waveform audio player & search filters
│       └── reports-dashboard/        # Hourly call volume & agent metrics
```

### Step 5.2: WebRTC Softphone Service (`webrtc-sip.service.ts`)
Using **SIP.js** to register directly to FreeSWITCH:
```typescript
import { UserAgent, Inviter, SessionState } from 'sip.js';

@Injectable({ providedIn: 'root' })
export class WebRtcSipService {
  private userAgent!: UserAgent;
  public currentSession: any = null;

  initializeSoftphone(extension: string, secret: string, wssUrl: string) {
    this.userAgent = new UserAgent({
      uri: UserAgent.makeURI(`sip:${extension}@telephony.company.com`),
      transportOptions: { server: wssUrl },
      authorizationUsername: extension,
      authorizationPassword: secret,
      delegate: {
        onInvite: (invitation) => this.handleIncomingCall(invitation)
      }
    });

    this.userAgent.start().then(() => {
      this.userAgent.register();
    });
  }

  answerCall() {
    if (this.currentSession) {
      this.currentSession.accept({
        sessionDescriptionHandlerOptions: {
          constraints: { audio: true, video: false }
        }
      });
    }
  }

  holdCall(hold: boolean) {
    // Media hold/unhold via re-INVITE
  }

  hangupCall() {
    if (this.currentSession) {
      this.currentSession.bye();
    }
  }
}
```

---

# Phase 6: Recording Lifecycle & Supervisor Coaching
**Goal:** Automate audio compression/archival and enable real-time supervisor intervention.

### Step 6.1: Recording Offloader Background Service (.NET)
1. FreeSWITCH writes `/recordings/raw/{CallId}.wav` (Stereo 16kHz).
2. The `.NET Worker Service` picks up the finalized file.
3. Transcodes WAV to Opus/MP3 using FFmpeg wrapper (`ffmpeg -i input.wav -c:a libopus -b:a 32k output.opus`).
4. Uploads to MinIO:
   ```csharp
   await minioClient.PutObjectAsync(new PutObjectArgs()
       .WithBucket("call-center-recordings")
       .WithObject($"v1/{DateTime.UtcNow:yyyy/MM/dd}/{callId}.opus")
       .WithFileName(tempOpusPath));
   ```
5. Updates SQL Server: Record marked as `IsArchived = 1` with StoragePath.
6. Deletes local temporary files.

### Step 6.2: Supervisor Coaching Engine
When a supervisor clicks an intervention mode in Angular, the backend executes an ESL command:
- **Silent Spy:**
  ```bash
  bgapi eavesdrop <CallUUID>
  ```
- **Whisper Coaching:**
  ```bash
  bgapi eavesdrop <CallUUID> w-leg
  ```
  *(The supervisor talks directly to the agent; caller audio is unmuted for supervisor, but caller cannot hear supervisor).*
- **Barge-In:**
  ```bash
  bgapi uuid_bridge <SupervisorCallUUID> <CallUUID>
  ```
  *(Converts into a 3-way conference).*

---

# Phase 7: Quality Assurance, Testing & Hardening
**Goal:** Verify voice quality, system resilience, and call handling under heavy concurrency.

### Step 7.1: Automated Testing Suites
1. **Unit Tests:** xUnit testing ACD queue distribution algorithms, state machines, and wrap-up countdown timers.
2. **SignalR Integration Tests:** Verify screen-pop payloads are delivered to target agent connections within 500ms.
3. **WebRTC End-to-End Tests:** Cypress / Playwright tests automating browser microphone permissions and call answer flows.

### Step 7.2: Telephony Load Testing with `SIPp`
Deploy `SIPp` on a dedicated testing machine to simulate load:
```bash
sipp -sn uac -d 30000 -s 029876543 192.168.10.10:5060 -r 10 -l 100 -m 1000
```
- Simulates 10 new calls per second (`-r 10`) holding up to 100 concurrent channels (`-l 100`).
- Monitor CPU, RAM, jitter buffer (<30ms), and packet loss (<0.5%).

---

# Phase 8: Production Deployment & Cutover
**Goal:** Deploy to production servers, test pilot numbers, and execute smooth agent migration.

### Step 8.1: Production Deployment Execution
1. Run database schema migrations on SQL Server AlwaysOn cluster.
2. Deploy Redis, RabbitMQ, and MinIO storage clusters.
3. Start FreeSWITCH container using `network_mode: host`.
4. Deploy .NET Core Web API and SignalR Hub containers behind Nginx reverse proxy.
5. Deploy Angular static production build (`ng build --configuration=production`) to Nginx edge servers.

### Step 8.2: Pilot Testing & Phased Cutover Plan
1. **Stage 1 (Internal Dialing):** Agents place test calls to internal extensions to verify microphone, speaker, and WebRTC stability.
2. **Stage 2 (Pilot Number):** Route a secondary BTCL pilot number to the new platform. Route 5 friendly customer service agents to handle live calls.
3. **Stage 3 (Department Cutover):** Shift 25 customer support agents to the platform. Validate CRM screen-pop and recording offload.
4. **Stage 4 (Full Cutover):** Migrate all 50+ agents and switch the primary BTCL hunt group to terminate 100% of traffic on the new platform.
5. **Stage 5 (Decommission):** Terminate the third-party software contract.

---

# Phase 9: AI Capabilities Expansion (Phase 2 & 3 Roadmap)
**Goal:** Ingest recordings and live streams into AI models for automated analytics.

```
+------------------------------------------------------------------------------------+
|  Phase 1 (MVP)         -->  Phase 2 (Post-MVP)       -->  Phase 3 (Enterprise AI)  |
|  - Inbound/Outbound calls   - Batch Audio Transcription   - Real-Time Live Assist  |
|  - Dual-Channel Audio       - LLM Call Summaries          - RAG Knowledge Base     |
|  - Manual QA Scorecards     - 100% Automated AI QA        - AI-Powered Routing     |
+------------------------------------------------------------------------------------+
```

1. **Batch AI Transcription Worker:** Consumes `call.audio.ready` events from RabbitMQ. Fetches stereo audio from MinIO and passes it through an offline Whisper / local Speech-to-Text model.
2. **LLM Summarization & Sentiment:** Analyzes transcripts to generate structured JSON summaries, key customer issues, and sentiment scores, automatically syncing them to the CRM contact record.
3. **Automated AI QA:** Evaluates compliance against mandatory greeting checklists, profanity detection, and customer dissatisfaction scoring across 100% of calls.
4. **Real-Time Agent Copilot:** Using FreeSWITCH `mod_audio_fork`, streams live 16kHz audio over WebSockets to provide agents with real-time FAQ suggestions and product hints on screen during active calls.

---

## Summary Checklist for Engineering Team

| Task ID | Task Description | Primary Owner | Status |
| :--- | :--- | :--- | :--- |
| **INF-01** | Terminate BTCL SIP Leased Line and configure network IPs | Network Engineer | Pending |
| **INF-02** | Provision Linux VMs, setup Docker & configure SSL certificates | DevOps Engineer | Pending |
| **TEL-01** | Install FreeSWITCH v1.10 & configure BTCL SIP Gateway | Telephony Engineer | Pending |
| **TEL-02** | Configure WebRTC WSS on port 7443 & Coturn STUN/TURN | Telephony Engineer | Pending |
| **DB-01**  | Run SQL Server migrations & initialize Redis/RabbitMQ/MinIO | Backend Lead | Pending |
| **BE-01**  | Build .NET Core Clean Architecture solution & ESL listener | Backend Engineer | Pending |
| **BE-02**  | Implement ACD Queue logic (Longest Idle Agent in Redis) | Backend Engineer | Pending |
| **BE-03**  | Implement SignalR CallHub & SupervisorHub | Backend Engineer | Pending |
| **FE-01**  | Build Angular WebRTC softphone service with SIP.js | Frontend Engineer | Pending |
| **FE-02**  | Build Agent Screen-Pop & Wrap-Up modal | Frontend Engineer | Pending |
| **FE-03**  | Build Supervisor Live Dashboard with Spy/Whisper/Barge | Frontend Engineer | Pending |
| **QA-01**  | Conduct SIPp load testing (100 concurrent calls) | QA Lead | Pending |
| **DEP-01** | Production pilot cutover (5 agents -> 50+ agents) | Solution Architect | Pending |
