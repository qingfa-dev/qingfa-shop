import * as stylex from '@stylexjs/stylex'

export const styles = stylex.create({
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: '24px',
    padding: '12px 24px',
    borderBottom: '1px solid #d0d7de',
    backgroundColor: '#ffffff',
  },
  brand: {
    fontSize: '18px',
    fontWeight: '700',
    color: '#1f2328',
  },
  nav: {
    display: 'flex',
    alignItems: 'center',
    gap: '16px',
  },
  navLink: {
    color: '#0969da',
    textDecoration: 'none',
    fontSize: '14px',
  },
  navPlaceholder: {
    color: '#8b949e',
    fontSize: '14px',
  },
  main: {
    padding: '24px',
  },
  footer: {
    display: 'flex',
    justifyContent: 'flex-end',
    padding: '8px 24px',
    borderTop: '1px solid #d0d7de',
  },
  chip: {
    fontSize: '12px',
    color: '#57606a',
  },
})
