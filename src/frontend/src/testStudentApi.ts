import { studentsApi } from "./api/students";

async function testStudentApi() {
  // Create a new student
  await studentsApi.createStudent({
    firstName: "Test",
    lastName: "Student",
    email: "test.student@example.com",
    studentNumber: "S99999",
    enrollmentDate: new Date().toISOString().slice(0, 10),
    notes: "Created by test script"
  });
  console.log("Student created.");

  // Retrieve all students
  const students = await studentsApi.getStudents();
  console.log("All students:", students);

  // Retrieve the student just created (by email match)
  const created = students.find(s => s.email === "test.student@example.com");
  if (created) {
    const student = await studentsApi.getStudent(String(created.studentId));
    console.log("Retrieved student:", student);
  } else {
    console.error("Created student not found in list.");
  }
}

// Run the test
// Note: Make sure a valid JWT is in localStorage before running this in the browser
// You can run this in the browser console or as a script in your React app

testStudentApi().catch(console.error);
