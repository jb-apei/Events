import { useInstructors } from '../hooks/useInstructors'
import type { Instructor } from '../api/instructors'

interface InstructorListProps {
  onSelectInstructor: (instructor: Instructor) => void
  selectedInstructor?: Instructor | null
}

const InstructorList = ({
  onSelectInstructor,
  selectedInstructor,
}: InstructorListProps) => {
  const { data: instructors, isLoading, error } = useInstructors()

  if (isLoading) {
    return (
      <div className="card">
        <h2>Instructors</h2>
        <div className="loading">Loading instructors...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="card">
        <h2>Instructors</h2>
        <div className="error-message">
          Failed to load instructors: {error.message}
        </div>
      </div>
    )
  }

  if (!instructors || !Array.isArray(instructors) || instructors.length === 0) {
    return (
      <div className="card">
        <h2>Instructors</h2>
        <div className="empty-state">
          <p>No instructors found</p>
          <p>Create your first instructor using the form</p>
        </div>
      </div>
    )
  }

  return (
    <div className="card">
      <h2>Instructors ({instructors.length})</h2>
      <div className="entity-list">
        {instructors.map((instructor) => (
          <div
            key={instructor.instructorId}
            className={`entity-item ${
              selectedInstructor?.instructorId === instructor.instructorId
                ? 'selected'
                : ''
            }`}
            onClick={() => onSelectInstructor(instructor)}
          >
            <h3>
              {instructor.firstName} {instructor.lastName}
            </h3>
            <p>📧 {instructor.email}</p>
            <p>🆔 {instructor.employeeNumber}</p>
            <p>🎓 {instructor.specialization || 'General'}</p>
            <p>
              <strong>Status:</strong> {instructor.status}
            </p>
            <p style={{ fontSize: '0.75rem', color: '#999' }}>
              Hired: {new Date(instructor.hireDate).toLocaleDateString()}
            </p>
          </div>
        ))}
      </div>
    </div>
  )
}

export default InstructorList
