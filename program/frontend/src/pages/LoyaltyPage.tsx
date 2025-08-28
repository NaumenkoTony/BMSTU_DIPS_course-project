import { useEffect, useState } from "react";
import { getLoyalty, type LoyaltyInfoResponse } from "../api/LoyaltyClient";
import { Card, Container, Loader, Text, Title } from "@mantine/core";
import "./LoyaltyPage.css";

export default function LoyaltyPage() {
  const [loyalty, setLoyalty] = useState<LoyaltyInfoResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const res = await getLoyalty();
        setLoyalty(res);
      } catch (err: any) {
        setError(err?.message ?? "Ошибка загрузки");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  return (
    <Container size="sm" className="loyalty-page">
      <Title order={2} mb="lg">Программа лояльности</Title>

      {loading && <Loader />}
      {error && <Text color="red">{error}</Text>}

      {loyalty && (
        <Card shadow="sm" radius="md" p="xl" withBorder>
          <Text size="lg" fw={500}>
            Ваш статус: <b>{loyalty.status}</b>
          </Text>
          <Text mt="sm">Текущая скидка: <b>{loyalty.discount}%</b></Text>
          <Text mt="sm">Количество бронирований: <b>{loyalty.reservationCount}</b></Text>
        </Card>
      )}
    </Container>
  );
}
