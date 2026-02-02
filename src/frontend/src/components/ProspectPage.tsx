import { useState } from 'react'
import { useWebSocket } from '../hooks/useWebSocket'
import { useInvalidateProspects } from '../hooks/useProspects'
import ProspectList from './ProspectList'
import ProspectForm from './ProspectForm'
import type { Prospect } from '../api/prospects'

const ProspectPage = () => {
  const [selectedProspect, setSelectedProspect] = useState<Prospect | null>(null)
  const invalidateProspects = useInvalidateProspects()

  // WebSocket connection with real-time event handling
  // Note: useWebSocket will automatically add the JWT token from localStorage
  const apiUrl = (import.meta as any).env?.VITE_API_URL || 'https://ca-events-api-gateway-dev.orangehill-95ada862.eastus2.azurecontainerapps.io/api'
  const wsUrl = apiUrl.replace(/^https?:/, 'wss:').replace('/api', '') + '/ws/events'
  const { status } = useWebSocket({
    url: wsUrl,
    onMessage: (event) => {
      // Handle ProspectCreated and ProspectUpdated events
      console.log('[ProspectPage] Received WebSocket event:', event)
      if (event.eventType === 'ProspectCreated' || event.eventType === 'ProspectUpdated') {
        console.log('[ProspectPage] Invalidating prospects cache for:', event.eventType)
        invalidateProspects()
      }
    },
    onConnect: () => {
      console.log('[ProspectPage] WebSocket connected successfully!')
    },
    onDisconnect: () => {
      console.log('[ProspectPage] WebSocket disconnected')
    },
  })

  const handleSelectProspect = (prospect: Prospect) => {
    setSelectedProspect(prospect)
  }

  const handleFormSuccess = () => {
    // Clear selection after successful form submission
    setSelectedProspect(null)
  }

  return (
    <div>
      <div
        className={`websocket-status ${status}`}
      >
        <span className="status-indicator"></span>
        WebSocket: {status.charAt(0).toUpperCase() + status.slice(1)}
      </div>

      <div className="prospect-page">
        <ProspectList
          onSelectProspect={handleSelectProspect}
          selectedProspect={selectedProspect}
        />
        <ProspectForm
          selectedProspect={selectedProspect}
          onSuccess={handleFormSuccess}
        />
      </div>
    </div>
  )
}

export default ProspectPage
