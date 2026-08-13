create extension if not exists pgcrypto;

create table if not exists public.operator_remote_devices (
  id uuid primary key default gen_random_uuid(),
  owner_id uuid not null references auth.users(id) on delete cascade,
  name text not null,
  secret_hash text not null,
  platform text not null default 'windows',
  app_version text not null default '1.0.0',
  last_seen_at timestamptz,
  revoked_at timestamptz,
  created_at timestamptz not null default now()
);

create index if not exists operator_remote_devices_owner_idx
  on public.operator_remote_devices(owner_id);

create table if not exists public.operator_pairing_sessions (
  id uuid primary key default gen_random_uuid(),
  code text not null,
  poll_token_hash text not null,
  device_name text not null,
  claimed_by uuid references auth.users(id) on delete set null,
  device_id uuid references public.operator_remote_devices(id) on delete set null,
  device_secret_once text,
  expires_at timestamptz not null,
  consumed_at timestamptz,
  created_at timestamptz not null default now()
);

create table if not exists public.operator_remote_commands (
  id uuid primary key default gen_random_uuid(),
  owner_id uuid not null references auth.users(id) on delete cascade,
  device_id uuid not null references public.operator_remote_devices(id) on delete cascade,
  command_text text not null,
  status text not null default 'queued' check (status in (
    'queued','awaiting_approval','running','paused','completed','failed','cancelled','rejected'
  )),
  control_state text not null default 'run' check (control_state in ('run','pause','cancel_requested')),
  approval_state text not null default 'approved' check (approval_state in ('approved','pending','rejected')),
  approval_reason text,
  capture_screenshot boolean not null default true,
  collect_result_files boolean not null default true,
  progress integer not null default 0 check (progress >= 0 and progress <= 100),
  result_text text,
  started_at timestamptz,
  finished_at timestamptz,
  created_at timestamptz not null default now()
);

create index if not exists operator_remote_commands_device_queue_idx
  on public.operator_remote_commands(device_id, status, created_at);
create index if not exists operator_remote_commands_owner_idx
  on public.operator_remote_commands(owner_id, created_at desc);

create table if not exists public.operator_remote_task_logs (
  id bigint generated always as identity primary key,
  command_id uuid not null references public.operator_remote_commands(id) on delete cascade,
  owner_id uuid not null references auth.users(id) on delete cascade,
  line text not null,
  created_at timestamptz not null default now()
);
create index if not exists operator_remote_task_logs_command_idx
  on public.operator_remote_task_logs(command_id, id);

create table if not exists public.operator_remote_artifacts (
  id uuid primary key default gen_random_uuid(),
  command_id uuid not null references public.operator_remote_commands(id) on delete cascade,
  owner_id uuid not null references auth.users(id) on delete cascade,
  kind text not null,
  file_name text not null,
  storage_path text not null,
  mime_type text,
  size_bytes bigint,
  created_at timestamptz not null default now()
);
create index if not exists operator_remote_artifacts_command_idx
  on public.operator_remote_artifacts(command_id, created_at);

create table if not exists public.operator_remote_push_tokens (
  id uuid primary key default gen_random_uuid(),
  owner_id uuid not null references auth.users(id) on delete cascade,
  token text not null,
  platform text,
  updated_at timestamptz not null default now(),
  created_at timestamptz not null default now(),
  unique(owner_id, token)
);

alter table public.operator_remote_devices enable row level security;
alter table public.operator_pairing_sessions enable row level security;
alter table public.operator_remote_commands enable row level security;
alter table public.operator_remote_task_logs enable row level security;
alter table public.operator_remote_artifacts enable row level security;
alter table public.operator_remote_push_tokens enable row level security;

drop policy if exists "owners read devices" on public.operator_remote_devices;
create policy "owners read devices" on public.operator_remote_devices for select to authenticated using (owner_id = auth.uid());

drop policy if exists "owners update devices" on public.operator_remote_devices;
create policy "owners update devices" on public.operator_remote_devices for update to authenticated
  using (owner_id = auth.uid()) with check (owner_id = auth.uid());

drop policy if exists "owners read commands" on public.operator_remote_commands;
create policy "owners read commands" on public.operator_remote_commands for select to authenticated using (owner_id = auth.uid());

drop policy if exists "owners read task logs" on public.operator_remote_task_logs;
create policy "owners read task logs" on public.operator_remote_task_logs for select to authenticated using (owner_id = auth.uid());

drop policy if exists "owners read artifacts" on public.operator_remote_artifacts;
create policy "owners read artifacts" on public.operator_remote_artifacts for select to authenticated using (owner_id = auth.uid());

drop policy if exists "owners manage push tokens" on public.operator_remote_push_tokens;
create policy "owners manage push tokens" on public.operator_remote_push_tokens for all to authenticated
  using (owner_id = auth.uid()) with check (owner_id = auth.uid());

insert into storage.buckets (id, name, public)
values ('operator-remote-artifacts', 'operator-remote-artifacts', false)
on conflict (id) do update set public = false;

drop policy if exists "owners read remote artifacts" on storage.objects;
create policy "owners read remote artifacts" on storage.objects for select to authenticated using (
  bucket_id = 'operator-remote-artifacts'
  and (storage.foldername(name))[1] = auth.uid()::text
);

do $$ begin
  alter publication supabase_realtime add table public.operator_remote_devices;
exception when duplicate_object then null; end $$;
do $$ begin
  alter publication supabase_realtime add table public.operator_remote_commands;
exception when duplicate_object then null; end $$;
do $$ begin
  alter publication supabase_realtime add table public.operator_remote_task_logs;
exception when duplicate_object then null; end $$;
do $$ begin
  alter publication supabase_realtime add table public.operator_remote_artifacts;
exception when duplicate_object then null; end $$;
