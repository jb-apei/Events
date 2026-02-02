import { useStudents } from '../hooks/useStudents'
import type { Student } from '../api/students'

interface StudentListProps {
  onSelectStudent: (student: Student) => void
  selectedStudent?: Student | null
}

const StudentList = ({ onSelectStudent, selectedStudent }: StudentListProps) => {
  const { data: students, isLoading, error } = useStudents()

  if (isLoading) {
    return (
      <div className="card">
        <h2>Students</h2>
        <div className="loading">Loading students...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="card">
        <h2>Students</h2>
        <div className="error-message">
          Failed to load students: {error.message}
        </div>
      </div>
    )
  }

  if (!students || !Array.isArray(students) || students.length === 0) {
    return (
      <div className="card">
        <h2>Students</h2>
        <div className="empty-state">
          <p>No students found</p>
          <p>Create your first student using the form</p>
        </div>
      </div>
    )
  }

  return (
    <div className="card">
      <h2>Students ({students.length})</h2>
      <div className="entity-list">
        {students.map((student) => (
          <div
            key={student.studentId}
            className={`entity-item ${
              selectedStudent?.studentId === student.studentId ? 'selected' : ''
            }`}
            onClick={() => onSelectStudent(student)}
          >
            <h3>
              {student.firstName} {student.lastName}
            </h3>
            <p>📧 {student.email}</p>
            <p>🆔 {student.studentNumber}</p>
            <p>
              <strong>Status:</strong> {student.status}
            </p>
            <p style={{ fontSize: '0.75rem', color: '#999' }}>
              Enrolled: {new Date(student.enrollmentDate).toLocaleDateString()}
            </p>
          </div>
        ))}
      </div>
    </div>
  )
}

export default StudentList
