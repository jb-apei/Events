import { useEffect, useRef, useState, useCallback } from 'react'

export type WebSocketStatus = 'connected' | 'disconnected' | 'connecting'

export interface EventEnvelope {
  eventId: string
  eventType: string
  schemaVersion: string
  occurredAt: string
  producer: string
  correlationId: string
  causationId: string
  subject: string
  data: any
}

export interface UseWebSocketOptions {
  url: string
  onMessage?: (event: EventEnvelope) => void
  onConnect?: () => void
  onDisconnect?: () => void
  reconnectInterval?: number
  maxReconnectAttempts?: number
}

export const useWebSocket = (options: UseWebSocketOptions) => {
  const {
    url,
    onMessage,
    onConnect,
    onDisconnect,
    reconnectInterval = 3000,
    maxReconnectAttempts = 10,
  } = options

  const [status, setStatus] = useState<WebSocketStatus>('disconnected')
  const wsRef = useRef<WebSocket | null>(null)
  const reconnectAttemptsRef = useRef(0)
  const reconnectTimeoutRef = useRef<NodeJS.Timeout | null>(null)
  const shouldConnectRef = useRef(true)

  // Use refs for callbacks to avoid re-connecting when they change
  const onMessageRef = useRef(onMessage)
  const onConnectRef = useRef(onConnect)
  const onDisconnectRef = useRef(onDisconnect)

  // Update refs when props change
  useEffect(() => {
    onMessageRef.current = onMessage
    onConnectRef.current = onConnect
    onDisconnectRef.current = onDisconnect
  }, [onMessage, onConnect, onDisconnect])

  const connect = useCallback(() => {
    if (!shouldConnectRef.current) return
    if (wsRef.current?.readyState === WebSocket.OPEN) return
    if (wsRef.current?.readyState === WebSocket.CONNECTING) return

    setStatus('connecting')

    try {
      const token = localStorage.getItem('jwt_token')
      const wsUrl = `${url}${token ? `?token=${encodeURIComponent(token)}` : ''}`

      const ws = new WebSocket(wsUrl)

      ws.onopen = () => {
        console.log('[WebSocket] Connected')
        setStatus('connected')
        reconnectAttemptsRef.current = 0
        onConnectRef.current?.()
      }

      ws.onmessage = (event) => {
        try {
          const envelope: EventEnvelope = JSON.parse(event.data)
          // console.log('[WebSocket] Received event:', envelope.eventType)
          onMessageRef.current?.(envelope)
        } catch (error) {
          console.error('[WebSocket] Failed to parse message:', error)
        }
      }

      ws.onerror = (error) => {
        console.error('[WebSocket] Error:', error)
      }

      ws.onclose = (event) => {
        // console.log('[WebSocket] Disconnected', event.code, event.reason)
        setStatus('disconnected')
        wsRef.current = null
        onDisconnectRef.current?.()

        // Don't reconnect on authentication failures (401-like codes) or policy violations
        if (event.code === 1008) { // Policy Violation (insufficient resources)
          console.log('[WebSocket] Connection rejected due to resource limits. Retrying in 10s...')
           // Wait longer before retrying if rejected
           reconnectTimeoutRef.current = setTimeout(() => {
             reconnectAttemptsRef.current = 0 
             connect()
           }, 10000)
           return
        }

        if (event.code === 1011) { // Server error
           shouldConnectRef.current = false
           return
        }

        // Attempt reconnection with exponential backoff
        if (shouldConnectRef.current && reconnectAttemptsRef.current < maxReconnectAttempts) {
          const delay = Math.min(reconnectInterval * Math.pow(2, reconnectAttemptsRef.current), 30000)
          
          reconnectTimeoutRef.current = setTimeout(() => {
            reconnectAttemptsRef.current++
            connect()
          }, delay)
        }
      }

      wsRef.current = ws
    } catch (error) {
      console.error('[WebSocket] Connection error:', error)
      setStatus('disconnected')
    }
  }, [url, reconnectInterval, maxReconnectAttempts]) // Removed callbacks from dependencies

  const disconnect = useCallback(() => {
    shouldConnectRef.current = false

    if (reconnectTimeoutRef.current) {
      clearTimeout(reconnectTimeoutRef.current)
      reconnectTimeoutRef.current = null
    }

    if (wsRef.current) {
      wsRef.current.close()
      wsRef.current = null
    }

    setStatus('disconnected')
  }, [])

  const send = useCallback((data: any) => {
    if (wsRef.current?.readyState === WebSocket.OPEN) {
      wsRef.current.send(JSON.stringify(data))
    } else {
      console.warn('[WebSocket] Cannot send message - not connected')
    }
  }, [])

  useEffect(() => {
    shouldConnectRef.current = true
    connect()

    return () => {
      disconnect()
    }
  }, [connect, disconnect])

  return {
    status,
    send,
    reconnect: connect,
    disconnect,
  }
}
