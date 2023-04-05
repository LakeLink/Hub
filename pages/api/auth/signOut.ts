import { withIronSessionApiRoute } from "iron-session/next";
import { NextApiRequest, NextApiResponse } from "next";
import { casV1Users } from "~/lib/cas";
import { sessionOptions } from "~/lib/session";

async function handler(request: NextApiRequest, response: NextApiResponse) {
  try {
    request.session.destroy();
    response.redirect(302, '/')
  } catch (error) {
    response.status(500).json({ message: (error as Error).message });
  }
}

export default withIronSessionApiRoute(handler, sessionOptions);
