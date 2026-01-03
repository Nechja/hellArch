import { ref, readonly, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'
import { useNotifications } from './useNotifications'

export interface CloudEvent {
  id: string
  source: string
  type: string
  time: string
  data?: Record<string, unknown>
}

type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting'

// Module-level singleton for the connection
let hubConnection: signalR.HubConnection | null = null
const connectionState = ref<ConnectionState>('disconnected')
const isInitialized = ref(false)

// Event handlers registry
const eventHandlers = new Map<string, Set<(event: CloudEvent) => void>>()

function createConnection(): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl('/hubs/events', {
      withCredentials: true,
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        const delays = [0, 2000, 5000, 10000, 30000]
        return delays[Math.min(retryContext.previousRetryCount, delays.length - 1)] ?? null
      },
    })
    .configureLogging(signalR.LogLevel.Warning)
    .build()
}

function setupConnectionHandlers(conn: signalR.HubConnection): void {
  const { info, warning, error } = useNotifications()

  conn.onreconnecting(() => {
    connectionState.value = 'reconnecting'
    warning('Connection lost', 'Attempting to reconnect...')
  })

  conn.onreconnected(() => {
    connectionState.value = 'connected'
    info('Reconnected', 'Connection restored')
  })

  conn.onclose(() => {
    connectionState.value = 'disconnected'
  })

  // Handle generic CloudEvents
  conn.on('ReceiveEvent', (event: CloudEvent) => {
    const handlers = eventHandlers.get(event.type)
    if (handlers) {
      handlers.forEach(handler => handler(event))
    }

    const wildcardHandlers = eventHandlers.get('*')
    if (wildcardHandlers) {
      wildcardHandlers.forEach(handler => handler(event))
    }
  })

  // Handle session invalidation
  conn.on('SessionInvalidated', (reason: string) => {
    error('Session Ended', reason || 'Your session has expired')
    setTimeout(() => {
      window.location.href = '/app/login'
    }, 2000)
  })

  // Handle user updates
  conn.on('UserUpdated', () => {
    info('Profile Updated', 'Your profile information has been updated')
    window.dispatchEvent(new CustomEvent('user-updated'))
  })
}

async function connect(): Promise<void> {
  if (hubConnection?.state === signalR.HubConnectionState.Connected) {
    return
  }

  if (!hubConnection) {
    hubConnection = createConnection()
    setupConnectionHandlers(hubConnection)
  }

  connectionState.value = 'connecting'

  try {
    await hubConnection.start()
    connectionState.value = 'connected'
    isInitialized.value = true
  } catch (err) {
    connectionState.value = 'disconnected'
    console.error('SignalR connection failed:', err)
    setTimeout(connect, 5000)
  }
}

async function disconnect(): Promise<void> {
  if (hubConnection) {
    await hubConnection.stop()
    connectionState.value = 'disconnected'
  }
}

async function subscribeToEventType(eventType: string): Promise<void> {
  if (hubConnection?.state === signalR.HubConnectionState.Connected) {
    await hubConnection.invoke('SubscribeToEventType', eventType)
  }
}

async function unsubscribeFromEventType(eventType: string): Promise<void> {
  if (hubConnection?.state === signalR.HubConnectionState.Connected) {
    await hubConnection.invoke('UnsubscribeFromEventType', eventType)
  }
}

function onEvent(eventType: string, handler: (event: CloudEvent) => void): () => void {
  if (!eventHandlers.has(eventType)) {
    eventHandlers.set(eventType, new Set())
  }
  eventHandlers.get(eventType)!.add(handler)

  return () => {
    eventHandlers.get(eventType)?.delete(handler)
  }
}

export function useSignalR() {
  onUnmounted(() => {
    // Keep connection alive across components
  })

  return {
    connectionState: readonly(connectionState),
    isConnected: () => connectionState.value === 'connected',
    connect,
    disconnect,
    subscribeToEventType,
    unsubscribeFromEventType,
    onEvent,
  }
}
