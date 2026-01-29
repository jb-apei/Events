import { useState } from 'react'
import InstructorForm from './InstructorForm'
import InstructorList from './InstructorList'
import type { Instructor } from '../api/instructors'

const InstructorPage = () => {
  const [selectedInstructor, setSelectedInstructor] = useState<Instructor | null>(null)

  const handleSelectInstructor = (instructor: Instructor) => {
    setSelectedInstructor(instructor)
  }

  const handleSuccess = () => {
    setSelectedInstructor(null)
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-8 text-gray-900">Instructor Management</h1>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        <div>
          <InstructorForm selectedInstructor={selectedInstructor} onSuccess={handleSuccess} />
        </div>

        <div>
          <InstructorList onSelectInstructor={handleSelectInstructor} />
        </div>
      </div>
    </div>
  )
}

export default InstructorPage
