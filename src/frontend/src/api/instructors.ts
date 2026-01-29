import axios, { AxiosInstance } from 'axios'

export interface Instructor {
  instructorId: number
  firstName: string
  lastName: string
  email: string
  phone?: string
  employeeNumber: string
  specialization?: string
  hireDate: string
  status: string
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface CreateInstructorRequest {
  firstName: string
  lastName: string
  email: string
  phone?: string
  employeeNumber: string
  specialization?: string
  hireDate: string
  notes?: string
}

export interface UpdateInstructorRequest {
  instructorId: string
  firstName?: string
  lastName?: string
  email?: string
  phone?: string
  specialization?: string
  status?: string
  notes?: string
}

class InstructorsApi {
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
          console.warn('[InstructorsApi] 401 Unauthorized - Token expired or invalid. Logging out.')
          localStorage.removeItem('jwt_token')
          sessionStorage.removeItem('auth_redirect')
          window.dispatchEvent(new CustomEvent('auth:logout'))
        }
        return Promise.reject(error)
      }
    )
  }

  async getInstructors(): Promise<Instructor[]> {
    const response = await this.client.get<Instructor[]>('/instructors')
    return response.data
  }

  async getInstructor(instructorId: string): Promise<Instructor> {
    const response = await this.client.get<Instructor>(`/instructors/${instructorId}`)
    return response.data
  }

  async createInstructor(request: CreateInstructorRequest): Promise<void> {
    await this.client.post('/instructors', request)
  }

  async updateInstructor(request: UpdateInstructorRequest): Promise<void> {
    await this.client.put(`/instructors/${request.instructorId}`, request)
  }
}

export const instructorsApi = new InstructorsApi()
