/**
 * DocsBadge Component for Mintlify Documentation
 *
 * A customizable badge component that matches Mintlify's design system.
 * Used to display member provenance (Extension, Inherited, Override, Virtual, Abstract).
 *
 * Usage:
 *   <DocsBadge text="Extension" variant="success" />
 *   <DocsBadge text="Inherited" variant="neutral" />
 *   <DocsBadge text="Override" variant="info" />
 *   <DocsBadge text="Virtual" variant="warning" />
 *   <DocsBadge text="Abstract" variant="warning" />
 */

export function DocsBadge({ text, variant = 'neutral' }) {
  // Tailwind color classes for consistent theming
  // Using standard Tailwind colors that work in both light and dark modes
  const variantClasses = {
    success: 'mint-bg-green-500/10 mint-text-green-600 dark:mint-text-green-400 mint-border-green-500/20',
    neutral: 'mint-bg-slate-500/10 mint-text-slate-600 dark:mint-text-slate-400 mint-border-slate-500/20',
    info: 'mint-bg-blue-500/10 mint-text-blue-600 dark:mint-text-blue-400 mint-border-blue-500/20',
    warning: 'mint-bg-amber-500/10 mint-text-amber-600 dark:mint-text-amber-400 mint-border-amber-500/20',
    danger: 'mint-bg-red-500/10 mint-text-red-600 dark:mint-text-red-400 mint-border-red-500/20'
  };

  const classes = variantClasses[variant] || variantClasses.neutral;

  return (
    <span
      className={`mint-inline-flex mint-items-center mint-px-2 mint-py-0.5 mint-rounded-full mint-text-xs mint-font-medium mint-tracking-wide mint-border mint-ml-1.5 mint-align-middle mint-whitespace-nowrap ${classes}`}
    >
      {text}
    </span>
  );
}