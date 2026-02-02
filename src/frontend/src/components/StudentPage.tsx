import { useState } from 'react'
import { useWebSocket } from '../hooks/useWebSocket'
import { useInvalidateStudents } from '../hooks/useStudents'
import StudentForm from './StudentForm'
import StudentList from './StudentList'
import type { Student } from '../api/students'

const StudentPage = () => {
  const [selectedStudent, setSelectedStudent] = useState<Student | null>(null)
  const invalidateStudents = useInvalidateStudents()

  // WebSocket connection with real-time event handling
  const apiUrl = (import.meta as any).env?.VITE_API_URL || 'https://ca-events-api-gateway-dev.orangehill-95ada862.eastus2.azurecontainerapps.io/api'
  const wsUrl = apiUrl.replace(/^https?:/, 'wss:').replace('/api', '') + '/ws/events'
  
  const { status } = useWebSocket({
    url: wsUrl,
    onMessage: (event) => {
      // Handle StudentCreated and StudentUpdated events
      if (event.eventType === 'StudentCreated' || event.eventType === 'StudentUpdated') {
        console.log('[StudentPage] Invalidating students cache for:', event.eventType)
        invalidateStudents()
      }
    },
  })

  const handleSelectStudent = (student: Student) => {
    setSelectedStudent(student)
  }

  const handleSuccess = () => {
    setSelectedStudent(null)
  }

  return (
    <div>
      <div className={`websocket-status ${status}`}>
        <span className="status-indicator"></span>
        WebSocket: {status.charAt(0).toUpperCase() + status.slice(1)}
      </div>

      <div className="entity-page">
        <StudentList 
          onSelectStudent={handleSelectStudent} 
          selectedStudent={selectedStudent}
        />
        <StudentForm 
          selectedStudent={selectedStudent} 
          onSuccess={handleSuccess} 
        />
      </div>
    </div>
  )
}

export default StudentPage
