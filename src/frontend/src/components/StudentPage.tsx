import { useState } from 'react'
import StudentForm from './StudentForm'
import StudentList from './StudentList'
import type { Student } from '../api/students'

const StudentPage = () => {
  const [selectedStudent, setSelectedStudent] = useState<Student | null>(null)

  const handleSelectStudent = (student: Student) => {
    setSelectedStudent(student)
  }

  const handleSuccess = () => {
    setSelectedStudent(null)
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-8 text-gray-900">Student Management</h1>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        <div>
          <StudentForm selectedStudent={selectedStudent} onSuccess={handleSuccess} />
        </div>

        <div>
          <StudentList onSelectStudent={handleSelectStudent} />
        </div>
      </div>
    </div>
  )
}

export default StudentPage
