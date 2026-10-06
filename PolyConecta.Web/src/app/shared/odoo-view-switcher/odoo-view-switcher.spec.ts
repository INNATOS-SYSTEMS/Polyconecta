import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { UiViewState } from '../../core/state/ui-view-state';
import { OdooViewSwitcher } from './odoo-view-switcher';

describe('OdooViewSwitcher', () => {
  it('cambia el modo de vista compartido', () => {
    const fixture = TestBed.createComponent(OdooViewSwitcher);
    fixture.detectChanges();
    const botones = fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>;
    expect(botones).toHaveLength(2);

    botones[1].click();
    expect(TestBed.inject(UiViewState).viewMode()).toBe('Kanban');
  });

  it('sin kanban solo ofrece la lista, deshabilitada y activa', () => {
    const fixture = TestBed.createComponent(OdooViewSwitcher);
    fixture.componentRef.setInput('kanbanEnabled', false);
    fixture.detectChanges();
    const botones = fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>;
    expect(botones).toHaveLength(1);
    expect(botones[0].disabled).toBe(true);
    expect(botones[0].classList).toContain('active');
  });
});
