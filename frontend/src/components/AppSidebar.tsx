import { useQueryClient } from '@tanstack/react-query'
import { ChevronDown, ChevronsUpDown, Globe, House, KeyRound, LogOut, MonitorSmartphone, Plus, Server, UserRound } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, NavLink, useLocation } from 'react-router'
import { toast } from 'sonner'
import { ArgusLogo } from '@/components/ArgusLogo'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSkeleton,
  useSidebar,
} from '@/components/ui/sidebar'
import { useApps } from '@/features/apps/queries'
import { signOut } from '@/features/auth/session'
import { useIsMobile } from '@/hooks/use-mobile'
import { initials } from '@/lib/format'
import { useAuthStore } from '@/stores/auth'

// Pill-shaped items: the active one is a white pill with a soft shadow
const pill =
  'h-10 rounded-full px-4 text-[0.9375rem] hover:bg-white/70 data-active:bg-white data-active:font-medium data-active:shadow-soft'

const nav = [
  { to: '/', label: 'Home', icon: House, end: true },
  { to: '/apps/new', label: 'New app', icon: Plus, end: true },
  { to: '/sessions', label: 'Your sessions', icon: MonitorSmartphone, end: true },
  { to: '/profile', label: 'Profile', icon: UserRound, end: true },
  { to: '/change-password', label: 'Password', icon: KeyRound, end: true },
]

export function AppSidebar() {
  const isMobile = useIsMobile()
  const { setOpenMobile } = useSidebar()
  const { pathname } = useLocation()
  const apps = useApps()

  // Close the mobile drawer after navigating
  useEffect(() => setOpenMobile(false), [pathname, setOpenMobile])

  return (
    // Inside the rounded frame on desktop; a drawer on phones
    <Sidebar collapsible={isMobile ? 'offcanvas' : 'none'} className="border-r border-sidebar-border/70">
      <SidebarHeader className="px-6 pt-7 pb-5">
        <ArgusLogo />
      </SidebarHeader>
      <SidebarContent className="px-3">
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu className="gap-1">
              {nav.map((item) => (
                <SidebarMenuItem key={item.to}>
                  <SidebarMenuButton asChild className={pill} isActive={item.end ? pathname === item.to : pathname.startsWith(item.to)}>
                    <NavLink to={item.to} end={item.end}>
                      <item.icon />
                      <span>{item.label}</span>
                    </NavLink>
                  </SidebarMenuButton>
                  {item.to === '/' && apps.data && apps.data.length > 0 && (
                    <SidebarMenuBadge className="top-2.5 right-3 size-5 rounded-full bg-ink p-0 text-[0.6875rem] text-white peer-hover/menu-button:text-white peer-data-active/menu-button:text-white">
                      {apps.data.length}
                    </SidebarMenuBadge>
                  )}
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <Collapsible defaultOpen className="group/collapsible">
          <SidebarGroup>
            <SidebarGroupLabel asChild className="h-9 px-4 text-sm font-semibold text-ink">
              <CollapsibleTrigger>
                Your apps
                <ChevronDown className="ml-auto transition-transform group-data-[state=closed]/collapsible:-rotate-90" />
              </CollapsibleTrigger>
            </SidebarGroupLabel>
            <CollapsibleContent>
              <SidebarGroupContent>
                <SidebarMenu className="gap-1">
                  {apps.isPending ? (
                    <>
                      <SidebarMenuSkeleton showIcon />
                      <SidebarMenuSkeleton showIcon />
                    </>
                  ) : apps.data?.length ? (
                    apps.data.map((app) => {
                      const Icon = app.isPublic ? Globe : Server
                      return (
                        <SidebarMenuItem key={app.id}>
                          <SidebarMenuButton asChild className={pill} isActive={pathname.startsWith(`/apps/${app.id}`)}>
                            <Link to={`/apps/${app.id}`}>
                              <Icon />
                              <span>{app.name}</span>
                            </Link>
                          </SidebarMenuButton>
                        </SidebarMenuItem>
                      )
                    })
                  ) : (
                    <p className="px-4 py-1 text-sm text-muted-foreground">No apps yet.</p>
                  )}
                </SidebarMenu>
              </SidebarGroupContent>
            </CollapsibleContent>
          </SidebarGroup>
        </Collapsible>
      </SidebarContent>
      <SidebarFooter className="p-3">
        <UserCard />
      </SidebarFooter>
    </Sidebar>
  )
}

/** Avatar, name and email with an account menu, and a log out pill */
function UserCard() {
  const queryClient = useQueryClient()
  const user = useAuthStore((s) => s.user)
  const [signingOut, setSigningOut] = useState(false)
  if (!user) return null

  const onSignOut = async () => {
    setSigningOut(true)
    try {
      await signOut(queryClient)
    } catch {
      toast.error('Could not reach the server, but you are signed out on this device.')
    }
  }

  return (
    <div className="grid gap-3 rounded-3xl bg-linear-to-b from-[#a9aeb9] to-[#8b919e] p-3 text-white shadow-soft">
      <DropdownMenu>
        <DropdownMenuTrigger className="flex items-center gap-3 rounded-2xl p-1 text-left outline-none focus-visible:ring-2 focus-visible:ring-white/70">
          <Avatar className="size-10 ring-2 ring-white/60">
            <AvatarFallback className="bg-white/90 font-medium text-ink">{initials(user.fullName)}</AvatarFallback>
          </Avatar>
          <span className="grid min-w-0 flex-1">
            <span className="truncate text-sm font-semibold">{user.fullName}</span>
            <span className="truncate text-xs text-white/80">{user.email}</span>
          </span>
          <ChevronsUpDown className="size-4 shrink-0 text-white/80" />
        </DropdownMenuTrigger>
        <DropdownMenuContent side="top" align="start" className="w-56">
          <DropdownMenuLabel className="truncate">{user.email}</DropdownMenuLabel>
          <DropdownMenuSeparator />
          <DropdownMenuItem asChild>
            <Link to="/profile">
              <UserRound /> Profile
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link to="/change-password">
              <KeyRound /> Change password
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link to="/sessions">
              <MonitorSmartphone /> Your sessions
            </Link>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
      <Button
        onClick={onSignOut}
        disabled={signingOut}
        className="h-11 w-full justify-start gap-3 rounded-full bg-white/25 pl-1.5 text-white backdrop-blur hover:bg-white/35"
      >
        <span className="grid size-8 place-items-center rounded-full bg-ink">
          <LogOut className="size-3.5" />
        </span>
        Log out
      </Button>
    </div>
  )
}
