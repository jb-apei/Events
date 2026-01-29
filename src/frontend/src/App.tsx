import { useState, useEffect } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom'
import LoginForm from './components/LoginForm'
import ProspectPage from './components/ProspectPage'
import StudentPage from './components/StudentPage'
import InstructorPage from './components/InstructorPage'
import OutboxDump from './components/OutboxDump'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30000,
      refetchOnWindowFocus: false,
    },
  },
})

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false)

  useEffect(() => {
    const token = localStorage.getItem('jwt_token')
    if (token) {
      setIsAuthenticated(true)
    }

    // Listen for auth:logout events (triggered by 401 responses)
    const handleLogout = () => {
      console.log('[App] Logout event received - clearing authentication')
      setIsAuthenticated(false)
    }

    window.addEventListener('auth:logout', handleLogout)
    return () => window.removeEventListener('auth:logout', handleLogout)
  }, [])

  const handleLoginSuccess = () => {
    setIsAuthenticated(true)
  }

  const handleLogout = () => {
    localStorage.removeItem('jwt_token')
    setIsAuthenticated(false)
  }

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <div className="app">
          <header className="app-header">
            <h1>Events - Identity Management System</h1>
            {isAuthenticated && (
              <div className="flex items-center gap-4">
                <nav className="flex gap-4">
                  <Link to="/" className="text-white hover:underline">Prospects</Link>
                  <Link to="/students" className="text-white hover:underline">Students</Link>
                  <Link to="/instructors" className="text-white hover:underline">Instructors</Link>
                  <Link to="/outboxdump" className="text-white hover:underline">Outbox Dump</Link>
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
