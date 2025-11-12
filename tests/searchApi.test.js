process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
const request = require('supertest');

const API_URL = 'https://localhost:5189';

describe('Search API', () => {
    let jwtToken = '';

    beforeAll(async () => {
      const loginRes = await request(API_URL)
        .post('/api/AccountApi/login')
        .send({ Username: 'ssherliann', Password: 'A~1234567890-' });
  
      expect(loginRes.status).toBe(200);
      expect(loginRes.body).toHaveProperty('token');
      jwtToken = loginRes.body.token;
    });

  it('should return all bookings if no filters provided', async () => {
    const res = await request(API_URL)
      .post('/api/search')
      .set('Authorization', `Bearer ${jwtToken}`)
      .send({}); 

    expect(res.status).toBe(200);
    expect(Array.isArray(res.body)).toBe(true);
  });

  it('should filter bookings by StartDate and EndDate', async () => {
    const res = await request(API_URL)
      .post('/api/search')
      .set('Authorization', `Bearer ${jwtToken}`)
      .send({
        startDate: "2025-11-01T00:00:00+02:00",
        endDate: "2025-11-30T23:59:59+02:00"
      });
    

    expect(res.status).toBe(200);
    expect(Array.isArray(res.body)).toBe(true);

    res.body.forEach(b => {
      if (b.StartTime && b.EndTime) { 
        const start = new Date(b.StartTime);
        const end = new Date(b.EndTime);

        expect(start >= new Date("2025-11-01T00:00:00+02:00")).toBe(true);
        expect(end <= new Date("2025-11-30T23:59:59+02:00")).toBe(true);
      }
    });
  });

  it('should filter bookings by Username', async () => {
    const res = await request(API_URL)
      .post('/api/search')
      .set('Authorization', `Bearer ${jwtToken}`)
      .send({
        username: "anna"
      });

    expect(res.status).toBe(200);
    expect(Array.isArray(res.body)).toBe(true);
    res.body.forEach(b => {
      expect(b.UserName.toLowerCase()).toContain("anna");
    });
  });

  it('should filter bookings by SpaceName', async () => {
    const res = await request(API_URL)
      .post('/api/search')
      .set('Authorization', `Bearer ${jwtToken}`)
      .send({ spaceName: "Conference" });

    expect(res.status).toBe(200);
    expect(Array.isArray(res.body)).toBe(true);
    res.body.forEach(b => {
      if (b.SpaceName) {
        expect(b.SpaceName.toLowerCase()).toContain("conference");
      }
    });
  });

  it('should combine multiple filters', async () => {
    const res = await request(API_URL)
      .post('/api/search')
      .set('Authorization', `Bearer ${jwtToken}`)
      .send({
        startDate: "2025-11-01T00:00:00+02:00",
        endDate: "2025-11-30T23:59:59+02:00",
        username: "anna",
        spaceName: "Conference"
      });

    expect(res.status).toBe(200);
    expect(Array.isArray(res.body)).toBe(true);
    res.body.forEach(b => {
      expect(new Date(b.StartTime) >= new Date("2025-11-01T00:00:00+02:00")).toBe(true);
      expect(new Date(b.EndTime) <= new Date("2025-11-30T23:59:59+02:00")).toBe(true);
      expect(b.UserName.toLowerCase()).toContain("anna");
      expect(b.SpaceName.toLowerCase()).toContain("conference");
    });
  });
});
