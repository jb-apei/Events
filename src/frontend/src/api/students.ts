import axios, { AxiosInstance } from 'axios'

export interface Student {
  studentId: number
  firstName: string
  lastName: string
  email: string
  phone?: string
  studentNumber: string
  status: string
  enrollmentDate: string
  expectedGraduationDate?: string
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface CreateStudentRequest {
  firstName: string
  lastName: string
  email: string
  phone?: string
  studentNumber: string
  enrollmentDate: string
  expectedGraduationDate?: string
  notes?: string
}

export interface UpdateStudentRequest {
  studentId: string
  firstName?: string
  lastName?: string
  email?: string
  phone?: string
  status?: string
  expectedGraduationDate?: string
  notes?: string
}

class StudentsApi {
  private client: AxiosInstance

  constructor() {
    this.client = axios.create({
      baseURL: '/api',
    })

    // Add JWT token to all requests
    this.client.interceptors.request.use((config) => {
      const token = localStorage.getItem('jwt_token')
      if (token) {
        config.headers.Authorization = `Bearer ${token}`
      }
      return config
    })

    // Handle 401 errors by clearing token and forcing logout
    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          console.warn('[StudentsApi] 401 Unauthorized - Token expired or invalid. Logging out.')
          localStorage.removeItem('jwt_token')
          sessionStorage.removeItem('auth_redirect')
          window.dispatchEvent(new CustomEvent('auth:logout'))
        }
        return Promise.reject(error)
      }
    )
  }

  async getStudents(): Promise<Student[]> {
    const response = await this.client.get<Student[]>('/students')
    return response.data
  }

  async getStudent(studentId: string): Promise<Student> {
    const response = await this.client.get<Student>(`/students/${studentId}`)
    return response.data
  }

  async createStudent(request: CreateStudentRequest): Promise<void> {
    await this.client.post('/students', request)
  }

  async updateStudent(request: UpdateStudentRequest): Promise<void> {
    await this.client.put(`/students/${request.studentId}`, request)
  }
}

export const studentsApi = new StudentsApi()
