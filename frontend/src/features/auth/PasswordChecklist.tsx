import { Check, X } from 'lucide-react'
import { cn } from '@/lib/utils'
import { passwordRules } from './schemas'

/** Live list of the password rules, ticked off as the user types */
export function PasswordChecklist({ password }: { password: string }) {
  return (
    <span className="grid gap-0.5" role="list" aria-label="Password requirements">
      {passwordRules.map((rule) => {
        const met = rule.test(password)
        const Icon = met ? Check : X
        return (
          <span role="listitem" key={rule.label} className={cn('flex items-center gap-1.5', met && 'text-foreground')}>
            <Icon className={cn('size-3.5', met && 'text-emerald-600')} aria-hidden />
            <span>
              {rule.label}
              <span className="sr-only">{met ? ' (met)' : ' (not met)'}</span>
            </span>
          </span>
        )
      })}
    </span>
  )
}
