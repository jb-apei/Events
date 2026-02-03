import { useState } from 'react'
import InstructorForm from './InstructorForm'
import InstructorList from './InstructorList'
import type { Instructor } from '../api/instructors'
import { useWebSocket } from '../hooks/useWebSocket'
import { useInvalidateInstructors } from '../hooks/useInstructors'

const InstructorPage = () => {
  const [selectedInstructor, setSelectedInstructor] = useState<Instructor | null>(
    null
  )
  const invalidateInstructors = useInvalidateInstructors()

  const apiUrl =
    (import.meta as any).env?.VITE_API_URL ||
    'https://ca-events-api-gateway-dev.orangehill-95ada862.eastus2.azurecontainerapps.io/api'
  const wsUrl =
    apiUrl.replace(/^https?:/, 'wss:').replace('/api', '') + '/ws/events'

  const { status } = useWebSocket({
    url: wsUrl,
    onMessage: (event) => {
      console.log('WS Message in InstructorPage:', event)
      if (
        event.eventType === 'InstructorCreated' ||
        event.eventType === 'InstructorUpdated'
      ) {
        invalidateInstructors()
      }
    },
  })

  const handleSelectInstructor = (instructor: Instructor) => {
    setSelectedInstructor(instructor)
  }

  const handleSuccess = () => {
    setSelectedInstructor(null)
  }

  const handleCancel = () => {
    setSelectedInstructor(null)
  }

  return (
    <div>
      <div className={`websocket-status ${status}`}>
        <span className="status-indicator"></span>
        WebSocket: {status.charAt(0).toUpperCase() + status.slice(1)}
      </div>

      <div className="entity-page">
        <InstructorList
          onSelectInstructor={handleSelectInstructor}
          selectedInstructor={selectedInstructor}
        />
        <InstructorForm
          selectedInstructor={selectedInstructor}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </div>
    </div>
  )
}

export default InstructorPage
