import { useState, useEffect } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom'
import LoginForm from './components/LoginForm'
import ProspectPage from './components/ProspectPage'
import StudentPage from './components/StudentPage'
import InstructorPage from './components/InstructorPage'
import OutboxDump from './components/OutboxDump'
import SBInspector from './components/SBInspector'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30000,
      refetchOnWindowFocus: false,
    },
  },
})

// Simple JWT parser (since we don't have jwt-decode installed)
const parseJwt = (token: string) => {
  try {
    const base64Url = token.split('.')[1]
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const jsonPayload = decodeURIComponent(
      window
        .atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    )
    return JSON.parse(jsonPayload)
  } catch (e) {
    return null
  }
}

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false)
  const [userEmail, setUserEmail] = useState<string>('')

  useEffect(() => {
    const token = localStorage.getItem('jwt_token')
    if (token) {
      setIsAuthenticated(true)
      const payload = parseJwt(token)
      if (payload && payload.email) {
        setUserEmail(payload.email)
      }
    }

    // Listen for auth:logout events (triggered by 401 responses)
    const handleLogout = () => {
      console.log('[App] Logout event received - clearing authentication')
      setIsAuthenticated(false)
      setUserEmail('')
    }

    window.addEventListener('auth:logout', handleLogout)
    return () => window.removeEventListener('auth:logout', handleLogout)
  }, [])

  const handleLoginSuccess = () => {
    const token = localStorage.getItem('jwt_token')
    if (token) {
      const payload = parseJwt(token)
      if (payload && payload.email) {
        setUserEmail(payload.email)
      }
    }
    setIsAuthenticated(true)
  }

  const handleLogout = () => {
    localStorage.removeItem('jwt_token')
    setIsAuthenticated(false)
    setUserEmail('')
  }

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <div className="app">
          <header className="app-header">
            <div>
              <h1>Events - Identity Management System</h1>
              <div style={{ fontSize: '0.75rem', opacity: 0.7, marginTop: '-5px' }}>
                Commit: {import.meta.env.VITE_APP_VERSION || 'Local'}
              </div>
            </div>
            {isAuthenticated && (
              <div className="flex items-center gap-4">
                {userEmail && (
                  <div style={{ fontSize: '0.9rem', marginRight: '1rem', opacity: 0.9 }}>
                    Hello, <strong>{userEmail}</strong>
                  </div>
                )}
                <nav className="flex gap-4">
                  <Link to="/" className="text-white hover:underline">Prospects</Link>
                  <Link to="/students" className="text-white hover:underline">Students</Link>
                  <Link to="/instructors" className="text-white hover:underline">Instructors</Link>
                  <Link to="/inspector" className="text-white hover:underline">Service Bus Inspector</Link>
                </nav>
                <button onClick={handleLogout} className="logout-btn">
                  Logout
                </button>
              </div>
            )}
          </header>
          <main className="app-main">
            {!isAuthenticated ? (
              <LoginForm onLoginSuccess={handleLoginSuccess} />
            ) : (
              <Routes>
                <Route path="/" element={<ProspectPage />} />
                <Route path="/students" element={<StudentPage />} />
                <Route path="/instructors" element={<InstructorPage />} />
                <Route path="/inspector" element={<SBInspector />} />
                <Route path="/outboxdump" element={<OutboxDump />} />
              </Routes>
            )}
          </main>
        </div>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App
